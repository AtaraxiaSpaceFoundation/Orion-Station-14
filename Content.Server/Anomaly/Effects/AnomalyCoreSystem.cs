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
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Components;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Containers;
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

        if (_solutions.TryGetSolution(args.Used, null, out var solutionEnt, out var solution)
            && TryHandleReactivationSolution(ent, args.User, solutionEnt.Value, solution, out var handled))
        {
            args.Handled = handled;
            return;
        }

        if (!_solutions.TryGetSolution(ent.Owner, "reactivation", out var coreSolutionEnt, out var coreSolution))
            return;

        if (TryHandleReactivationSolution(ent, args.User, coreSolutionEnt.Value, coreSolution, out handled))
            args.Handled = handled;
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
        var attempted = false;

        foreach (var reagent in ent.Comp.ReactivationReagents)
        {
            var reagentId = new ReagentId(reagent, null);
            if (!solution.TryGetReagentQuantity(reagentId, out var quantity))
                continue;

            attempted = true;
            if (quantity < ent.Comp.ReactivationReagentAmount)
                continue;

            _solutions.RemoveReagent(solutionEnt, reagentId, ent.Comp.ReactivationReagentAmount);
            if (!ReactivateCore(ent))
            {
                handled = false;
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

            attempted = true;
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
                handled = false;
                return true;
            }

            PopupResult(ent, user, "anomaly-core-reactivated", PopupType.Medium);
            handled = true;
            return true;
        }

        if (attempted)
        {
            PopupResult(ent, user, "anomaly-core-reactivation-failed", PopupType.SmallCaution);
            handled = false;
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
        var target = ResolveHazardTarget(ent.Owner, user);
        var popupTarget = user ?? target;
        var effect = _random.Next(5);
        switch (effect)
        {
            case 0:
                var damage = new DamageSpecifier
                {
                    DamageDict = new Dictionary<string, FixedPoint2>
                    {
                        ["Heat"] = 15,
                        ["Poison"] = 10,
                    },
                };
                _damageable.TryChangeDamage(target, damage, true, origin: ent);
                break;
            case 1:
                _flammable.Ignite(target, ent);
                break;
            case 2:
                _electrocution.TryDoElectrocution(target, ent, 30, TimeSpan.FromSeconds(2), true, ignoreInsulation: false);
                break;
            case 3:
                if (ent.Comp.HazardousAnomalyPrototype is { } anomalyPrototype)
                    Spawn(anomalyPrototype, Transform(ent).Coordinates);
                QueueDel(ent);
                break;
            default:
                QueueDel(ent);
                break;
        }

        PopupResult(ent, popupTarget, "anomaly-core-reactivation-hazard", PopupType.MediumCaution);
    }

    private EntityUid ResolveHazardTarget(EntityUid core, EntityUid? user)
    {
        if (user is { } userUid)
            return userUid;

        var current = core;
        while (_container.TryGetContainingContainer((current, null, null), out var container))
        {
            current = container.Owner;
        }

        return current;
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
