// SPDX-FileCopyrightText: 2025 Coenx-flex
// SPDX-FileCopyrightText: 2025 Cojoke
// SPDX-FileCopyrightText: 2025 ark1368
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using Content.Goobstation.Shared.Devil;
using Content.Goobstation.Shared.SlaughterDemon;
using Content.Server.Medical;
using Content.Shared._EinsteinEngines.Silicon.Components;
using Content.Shared._Orion.CorticalBorer;
using Content.Shared._Orion.CorticalBorer.Components;
using Content.Shared._Orion.Morph;
using Content.Shared._White.Xenomorphs.Xenomorph;
using Content.Shared.Body.Components;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;

namespace Content.Server._Orion.CorticalBorer;

public sealed partial class CorticalBorerSystem
{
    [Dependency] private readonly VomitSystem _vomit = default!;

    private void SubscribeAbilities()
    {
        SubscribeLocalEvent<CorticalBorerComponent, CorticalInfestEvent>(OnInfest);
        SubscribeLocalEvent<CorticalBorerComponent, CorticalInfestDoAfterEvent>(OnInfestDoAfter);

        SubscribeLocalEvent<CorticalBorerComponent, CorticalEjectEvent>(OnEjectHost);
        SubscribeLocalEvent<CorticalBorerComponent, CorticalTakeControlEvent>(OnTakeControl);
        SubscribeLocalEvent<CorticalBorerComponent, CorticalForceSpeakEvent>(OnForceSpeak);
        SubscribeLocalEvent<CorticalBorerComponent, CorticalParalyzeHostEvent>(OnParalyzeHost);
        SubscribeLocalEvent<CorticalBorerComponent, CorticalWillingHostEvent>(OnWillingHost);

        SubscribeLocalEvent<CorticalBorerComponent, CorticalChemMenuActionEvent>(OnChemicalMenu);
        SubscribeLocalEvent<CorticalBorerComponent, CorticalCheckBloodEvent>(OnCheckBlood);

        SubscribeLocalEvent<CorticalBorerInfestedComponent, CorticalEndControlEvent>(OnEndControl);
        SubscribeLocalEvent<CorticalBorerComponent, CorticalEndControlEvent>(OnEndControlByVoluntaryHost);
        SubscribeLocalEvent<CorticalBorerComponent, CorticalLayEggEvent>(OnLayEgg);
    }

    private void OnChemicalMenu(Entity<CorticalBorerComponent> ent, ref CorticalChemMenuActionEvent args)
    {
        if(!TryComp<UserInterfaceComponent>(ent, out var uic))
            return;

        if (!TryGetHost(ent, out var host))
            return;

        if (!CanUseAbility(ent, host.Value))
            return;

        UpdateUiState(ent);
        UI.TryToggleUi((ent, uic), CorticalBorerDispenserUiKey.Key, ent);
        args.Handled = true;
    }

    private void OnInfest(Entity<CorticalBorerComponent> ent, ref CorticalInfestEvent args)
    {
        var (uid, comp) = ent;
        var target = args.Target;
        var targetIdentity = Identity.Entity(target, EntityManager);

        if (comp.Host is not null)
        {
            Popup.PopupEntity(Loc.GetString("cortical-borer-has-host"), uid, uid, PopupType.Medium);
            return;
        }

        if (HasComp<CorticalBorerInfestedComponent>(target))
        {
            Popup.PopupEntity(Loc.GetString("cortical-borer-host-already-infested", ("target", targetIdentity)), uid, uid, PopupType.Medium);
            return;
        }

        if (IsInvalidHost(target))
        {
            Popup.PopupEntity(Loc.GetString("cortical-borer-invalid-host", ("target", targetIdentity)), uid, uid, PopupType.Medium);
            return;
        }

        // target is on sugar for some reason, can't go in there
        if (!CanUseAbility(ent, target))
            return;

        var infestAttempt = new InfestHostAttempt();
        RaiseLocalEvent(target, infestAttempt);

        if (infestAttempt.Cancelled)
        {
            Popup.PopupEntity(Loc.GetString("cortical-borer-face-covered", ("target", targetIdentity)), uid, uid, PopupType.Medium);
            return;
        }

        Popup.PopupEntity(Loc.GetString("cortical-borer-start-infest", ("target", targetIdentity)), uid, uid, PopupType.Medium);

        var infestArgs = new DoAfterArgs(EntityManager, uid, TimeSpan.FromSeconds(3), new CorticalInfestDoAfterEvent(), uid, target)
        {
            DistanceThreshold = 1.5f,
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnWeightlessMove = true,
            AttemptFrequency = AttemptFrequency.StartAndEnd,
            Hidden = true,
        };

        _doAfter.TryStartDoAfter(infestArgs);
    }

    // Anything with bloodstream, BUT NOT THIS!!!
    private bool IsInvalidHost(EntityUid target)
    {
        return !HasComp<BloodstreamComponent>(target) ||
               HasComp<CorticalBorerComponent>(target) ||
               HasComp<MorphComponent>(target) ||
               HasComp<DevilComponent>(target) ||
               HasComp<SlaughterDemonComponent>(target) ||
               HasComp<XenomorphComponent>(target) ||
               HasComp<SiliconComponent>(target);
    }

    private void OnInfestDoAfter(Entity<CorticalBorerComponent> ent, ref CorticalInfestDoAfterEvent args)
    {
        if (args.Handled)
            return;

        if (args.Args.Target is not { } target)
            return;

        if (args.Cancelled || HasComp<CorticalBorerInfestedComponent>(target))
            return;

        if (!CanUseAbility(ent, target))
            return;

        if (IsInvalidHost(target))
            return;

        InfestTarget(ent, target);
        _admin.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(ent):actor} infested {ToPrettyString(target):target}");
        args.Handled = true;
    }

    private void OnEjectHost(Entity<CorticalBorerComponent> ent, ref CorticalEjectEvent args)
    {
        if (args.Handled)
            return;

        var (uid, comp) = ent;

        if (comp.Host is null)
        {
            Popup.PopupEntity(Loc.GetString("cortical-borer-no-host"), uid, uid, PopupType.Medium);
            return;
        }

        // My boy too weak under sugar and cannot eject from host!
        if (!CanUseAbility(ent, comp.Host.Value))
            return;

        var oldHost = comp.Host.Value;
        if (!TryEjectBorer(ent))
            return;

        _admin.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(ent):actor} ejected from host {ToPrettyString(oldHost):target}");
        args.Handled = true;
    }

    private void OnForceSpeak(Entity<CorticalBorerComponent> ent, ref CorticalForceSpeakEvent args)
    {
        if (args.Handled)
            return;

        if (!TryGetHost(ent, out var host))
            return;

        if (!CanUseAbility(ent, host.Value))
            return;

        if (!TryComp<UserInterfaceComponent>(ent, out var uic))
            return;

        UI.TryToggleUi((ent, uic), CorticalBorerForceSpeakUiKey.Key, ent);

        args.Handled = true;
    }

    private void OnWillingHost(Entity<CorticalBorerComponent> ent, ref CorticalWillingHostEvent args)
    {
        if (args.Handled)
            return;

        if (!TryGetHost(ent, out var host))
            return;

        if (!CanUseAbility(ent, host.Value))
            return;

        AskForWillingHost(ent);
        args.Handled = true;
    }

    private void OnParalyzeHost(Entity<CorticalBorerComponent> ent, ref CorticalParalyzeHostEvent args)
    {
        if (args.Handled)
            return;

        if (!TryGetHost(ent, out var host))
            return;

        if (IsDeadHost(host.Value))
            return;

        if (!CanUseAbility(ent, host.Value))
            return;

        _stun.TryUpdateParalyzeDuration(host.Value, ent.Comp.ParalyzeHostDuration);
        _admin.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(ent):actor} paralyzed host {ToPrettyString(host.Value):target}");
        args.Handled = true;
    }

    private void OnCheckBlood(Entity<CorticalBorerComponent> ent, ref CorticalCheckBloodEvent args)
    {
        if (args.Handled)
            return;

        if (!TryGetHost(ent, out _))
            return;

        if (TryToggleCheckBlood(ent))
            args.Handled = true;
    }

    private void OnTakeControl(Entity<CorticalBorerComponent> ent, ref CorticalTakeControlEvent args)
    {
        if (args.Handled)
            return;

        if (!TryGetHost(ent, out var host))
            return;

        // Host is dead, you can't take control
        if (IsDeadHost(host.Value))
            return;

        if (!TryComp<CorticalBorerInfestedComponent>(host.Value, out var infestedComp))
            return;

        if (!CanUseAbility(ent, host.Value))
            return;

        // IDK how you would cause this...
        if (ent.Comp.ControllingHost)
        {
            Popup.PopupEntity(Loc.GetString("cortical-borer-already-control"), ent, ent, PopupType.Medium);
            return;
        }

        TakeControlHost(ent, infestedComp);

        args.Handled = true;
    }

    private void OnEndControl(Entity<CorticalBorerInfestedComponent> host, ref CorticalEndControlEvent args)
    {
        if (args.Handled)
            return;

        EndControl(host);

        args.Handled = true;
    }

    private void OnEndControlByVoluntaryHost(Entity<CorticalBorerComponent> ent, ref CorticalEndControlEvent args)
    {
        if (args.Handled)
            return;

        if (!TryGetHost(ent, out var host))
            return;

        if (!ent.Comp.WillingHosts.Contains(host.Value))
            return;

        if (!TryComp<CorticalBorerInfestedComponent>(host.Value, out var infestedComp))
            return;

        EndControl((host.Value, infestedComp));
        args.Handled = true;
    }

    private void OnLayEgg(Entity<CorticalBorerComponent> borer, ref CorticalLayEggEvent args)
    {
        if (args.Handled)
            return;

        if (!borer.Comp.CanReproduce)
            return;

        if (!TryGetHost(borer, out var host))
            return;

        if (!CanUseAbility(borer, host.Value))
            return;

        if (borer.Comp.EggCost > borer.Comp.ChemicalPoints)
        {
            Popup.PopupEntity(Loc.GetString("cortical-borer-not-enough-chem"), borer, borer, PopupType.Medium);
            return;
        }

        _vomit.Vomit(host.Value, -20, -20); // half as much chem vomit, a lot that is coming up is the egg
        LayEgg(borer);
        borer.Comp.EggsLaid++;
        Dirty(borer);
        UpdateChemicals(borer, -borer.Comp.EggCost);
        _admin.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(borer):actor} laid an egg in host {ToPrettyString(host.Value):target}");

        args.Handled = true;
    }

    private bool TryGetHost(Entity<CorticalBorerComponent> ent, [NotNullWhen(true)] out EntityUid? host)
    {
        host = ent.Comp.Host;
        if (host is not null)
            return true;

        Popup.PopupEntity(Loc.GetString("cortical-borer-no-host"), ent, ent, PopupType.Medium);
        return false;
    }

    private bool IsDeadHost(EntityUid host)
    {
        return TryComp<MobStateComponent>(host, out var mobState) && mobState.CurrentState == MobState.Dead;
    }
}
