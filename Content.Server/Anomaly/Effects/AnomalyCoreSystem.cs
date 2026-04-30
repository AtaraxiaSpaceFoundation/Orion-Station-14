// SPDX-FileCopyrightText: 2023 Ed <96445749+TheShuEd@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: MIT

using Content.Goobstation.Maths.FixedPoint;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Cargo.Systems;
using Content.Server.Electrocution;
using Content.Shared.Anomaly.Components;
using Content.Shared.Atmos.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Components;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Anomaly.Effects;

/// <summary>
/// This component reduces the value of the entity during decay
/// </summary>
public sealed class AnomalyCoreSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    // Orion-Start
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly ElectrocutionSystem _electrocution = default!;
    [Dependency] private readonly FlammableSystem _flammable = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    // Orion-End

    private readonly HashSet<EntityUid> _processingInjected = new(); // Orion

    public override void Initialize()
    {
        SubscribeLocalEvent<AnomalyCoreComponent, PriceCalculationEvent>(OnGetPrice);
        // Orion-Start
        SubscribeLocalEvent<AnomalyCoreComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
        SubscribeLocalEvent<AnomalyCoreComponent, SolutionContainerChangedEvent>(OnSolutionChanged);
        // Orion-End
    }

    private void OnGetPrice(Entity<AnomalyCoreComponent> core, ref PriceCalculationEvent args)
    {
        var timeLeft = core.Comp.DecayMoment - _gameTiming.CurTime;
        var lerp = timeLeft.TotalSeconds / core.Comp.TimeToDecay;
        lerp = Math.Clamp(lerp, 0, 1);

        args.Price = MathHelper.Lerp(core.Comp.EndPrice, core.Comp.StartPrice, lerp);
    }

    // Orion-Start
    #region Reactivation
    private void OnAfterInteractUsing(Entity<AnomalyCoreComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (args.Handled || !args.CanReach || !ent.Comp.IsDecayed)
            return;

        if (!_solutions.TryGetSolution(ent.Owner, "reactivation", out var coreSolutionEnt, out var coreSolution))
            return;

        if (TryHandleReactivationSolution(ent, args.User, coreSolutionEnt.Value, coreSolution, out var handled))
        {
            args.Handled = handled;
            return;
        }

        PopupResult(ent, args.User, "anomaly-core-reactivation-failed", PopupType.SmallCaution);
        args.Handled = true;
    }

    private void OnSolutionChanged(Entity<AnomalyCoreComponent> ent, ref SolutionContainerChangedEvent args)
    {
        if (args.SolutionId != "reactivation" || !ent.Comp.IsDecayed || _processingInjected.Contains(ent))
            return;

        if (!_solutions.TryGetSolution(ent.Owner, "reactivation", out var solutionEnt, out var solution))
            return;

        _processingInjected.Add(ent);
        try
        {
            TryHandleReactivationSolution(ent, null, solutionEnt.Value, solution, out _);
        }
        finally
        {
            _processingInjected.Remove(ent);
        }
    }

    private bool TryHandleReactivationSolution(Entity<AnomalyCoreComponent> ent, EntityUid? user, Entity<SolutionComponent> solutionEnt, Solution solution, out bool handled)
    {
        foreach (var reagent in ent.Comp.ReactivationReagents)
        {
            var reagentId = new ReagentId(reagent, null);
            if (!solution.TryGetReagentQuantity(reagentId, out var quantity))
                continue;

            if (quantity < ent.Comp.ReactivationReagentAmount)
                continue;

            _solutions.RemoveReagent(solutionEnt, reagentId, ent.Comp.ReactivationReagentAmount);
            if (!ReactivateCore(ent))
            {
                handled = true;
                return true;
            }

            PopupResult(ent, user, "anomaly-core-reactivated", PopupType.Medium);
            handled = true;
            return true;
        }

        foreach (var reagent in ent.Comp.HazardousReactivationReagents)
        {
            var reagentId = new ReagentId(reagent, null);
            if (!solution.TryGetReagentQuantity(reagentId, out var quantity))
                continue;

            if (quantity < ent.Comp.ReactivationReagentAmount)
                continue;

            _solutions.RemoveReagent(solutionEnt, reagentId, ent.Comp.ReactivationReagentAmount);

            if (_random.Prob(ent.Comp.HazardousFailureChance))
            {
                DoHazardousFailure(ent, user);
                handled = true;
                return true;
            }

            if (!ReactivateCore(ent))
            {
                handled = true;
                return true;
            }

            PopupResult(ent, user, "anomaly-core-reactivated", PopupType.Medium);
            handled = true;
            return true;
        }

        handled = false;
        return false;
    }

    private bool ReactivateCore(Entity<AnomalyCoreComponent> ent)
    {
        if (ent.Comp.ReactivationPrototype is not { } activePrototypeId)
            return false;

        var reactivated = Spawn(activePrototypeId, Transform(ent).Coordinates);

        if (_container.TryGetContainingContainer((ent.Owner, null, null), out var container))
            _container.Insert(reactivated, container, force: true);

        QueueDel(ent);
        return true;
    }

    private void DoHazardousFailure(Entity<AnomalyCoreComponent> ent, EntityUid? user)
    {
        var targets = ResolveHazardTargets(ent.Owner);
        var popupTarget = user ?? ent.Owner;

        var effects = new List<Func<bool>>
        {
            () => TryApplyHazardDamage(targets, ent),
            () => TryIgniteHazardTargets(targets, ent),
            () => TryElectrocuteHazardTargets(targets, ent),
            () => TrySpawnHazardousAnomaly(ent),
        };

        var effect = effects[_random.Next(effects.Count)];
        var effectApplied = effect();
        if (!effectApplied)
            TrySpawnHazardousAnomaly(ent);

        PopupResult(ent, popupTarget, "anomaly-core-reactivation-hazard", PopupType.MediumCaution);
    }

    private List<EntityUid> ResolveHazardTargets(EntityUid core)
    {
        var targets = new List<EntityUid>();
        var coordinates = Transform(core).Coordinates;

        foreach (var uid in _lookup.GetEntitiesInRange(coordinates, 3f))
        {
            if (uid == core || !HasComp<ActorComponent>(uid))
                continue;

            targets.Add(uid);
        }

        return targets;
    }

    private bool TryApplyHazardDamage(List<EntityUid> targets, Entity<AnomalyCoreComponent> core)
    {
        var applied = false;
        var damage = new DamageSpecifier
        {
            DamageDict = new Dictionary<string, FixedPoint2>
            {
                ["Heat"] = 15,
                ["Poison"] = 10,
            },
        };

        foreach (var targetUid in targets)
        {
            if (!HasComp<DamageableComponent>(targetUid))
                continue;

            applied |= _damageable.TryChangeDamage(targetUid, damage, origin: core) != null;
        }

        return applied;
    }

    private bool TryIgniteHazardTargets(List<EntityUid> targets, Entity<AnomalyCoreComponent> core)
    {
        var applied = false;
        foreach (var targetUid in targets)
        {
            if (!HasComp<FlammableComponent>(targetUid))
                continue;

            _flammable.Ignite(targetUid, core);
            applied = true;
        }

        return applied;
    }

    private bool TryElectrocuteHazardTargets(List<EntityUid> targets, Entity<AnomalyCoreComponent> core)
    {
        var applied = false;
        foreach (var targetUid in targets)
        {
            applied |= _electrocution.TryDoElectrocution(targetUid, core, 30, TimeSpan.FromSeconds(2), true, ignoreInsulation: false);
        }

        return applied;
    }

    private bool TrySpawnHazardousAnomaly(Entity<AnomalyCoreComponent> ent)
    {
        if (ent.Comp.HazardousAnomalyPrototype is not { } anomalyPrototype)
            return false;

        Spawn(anomalyPrototype, Transform(ent).Coordinates);
        QueueDel(ent);
        return true;
    }

    private void PopupResult(EntityUid core, EntityUid? user, string message, PopupType popupType)
    {
        if (user is { } userUid)
            _popup.PopupEntity(Loc.GetString(message), core, userUid, popupType);
        else
            _popup.PopupEntity(Loc.GetString(message), core, popupType);
    }
    #endregion
    // Orion-End
}
