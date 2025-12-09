// SPDX-FileCopyrightText: 2025 Coenx-flex
// SPDX-FileCopyrightText: 2025 Cojoke
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Orion.CorticalBorer.Components;
using Content.Shared._Shitmed.Targeting;
using Content.Shared.Actions;
using Content.Shared.MedicalScanner;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;

namespace Content.Shared._Orion.CorticalBorer;

[Virtual]
public class SharedCorticalBorerSystem : EntitySystem
{
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ISerializationManager _serManager = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;
    [Dependency] protected readonly SharedUserInterfaceSystem UI = default!;
    [Dependency] protected readonly SharedActionsSystem Actions = default!;
    [Dependency] protected readonly SharedContainerSystem Container = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public bool CanUseAbility(Entity<CorticalBorerComponent> ent, EntityUid target)
    {
        if (!_statusEffects.HasStatusEffect(target,"CorticalBorerProtection")) // hardcoded the status effect because...
            return true;

        Popup.PopupEntity(Loc.GetString("cortical-borer-sugar-block"), ent.Owner, ent.Owner, PopupType.Medium);
        return false;

    }

    public void InfestTarget(Entity<CorticalBorerComponent> ent, EntityUid target)
    {
        var (uid, comp) = ent;

        // Make sure the infected person is infected right
        var infestedComp = EnsureComp<CorticalBorerInfestedComponent>(target);

        // Make sure they get into the target
        if (!Container.Insert(uid, infestedComp.InfestationContainer))
        {
            RemCompDeferred<CorticalBorerInfestedComponent>(target); // oh no it didn't work somehow so remove the comp you just added...
            return;
        }

        // Set up the Borer
        infestedComp.Borer = ent;
        comp.Host = target;

        if (comp.AddOnInfest is not null)
        {
            foreach (var (_, compReg) in comp.AddOnInfest)
            {
                var compType = compReg.Component.GetType();
                if (HasComp(ent, compType))
                    continue;

                var newComp = (Component) _serManager.CreateCopy(compReg.Component, notNullableOverride: true);
                EntityManager.AddComponent(ent, newComp, true);
            }
        }

        if (comp.RemoveOnInfest is not null)
        {
            foreach (var (_, compReg) in comp.RemoveOnInfest)
            {
                RemCompDeferred(ent, compReg.Component.GetType());
            }
        }

        if (TryComp<DamageableComponent>(ent, out var damComp))
            _damage.SetAllDamage(ent, damComp, 0);
    }

    public bool TryEjectBorer(Entity<CorticalBorerComponent> ent)
    {
        if (!ent.Comp.Host.HasValue)
            return false;

        if (TerminatingOrDeleted(ent.Owner))
            return false;

        // Make sure they get out of the host
        if (!Container.TryRemoveFromContainer(ent.Owner))
            return false;

        // close all the UIs that relate to host
        if (TryComp<UserInterfaceComponent>(ent, out var uic))
        {
            UI.CloseUi((ent.Owner,uic), HealthAnalyzerUiKey.Key);
            UI.CloseUi((ent.Owner,uic), CorticalBorerDispenserUiKey.Key);
        }

        RemCompDeferred<CorticalBorerInfestedComponent>(ent.Comp.Host.Value);
        ent.Comp.Host = null;

        if (ent.Comp.RemoveOnInfest is not null)
        {
            foreach (var (_, compReg) in ent.Comp.RemoveOnInfest)
            {
                var compType = compReg.Component.GetType();
                if (HasComp(ent, compType))
                    continue;

                var newComp = (Component) _serManager.CreateCopy(compReg.Component, notNullableOverride: true);
                EntityManager.AddComponent(ent, newComp, true);
            }
        }

        if (ent.Comp.AddOnInfest is not null)
        {
            foreach (var (_, compReg) in ent.Comp.AddOnInfest)
            {
                RemCompDeferred(ent, compReg.Component.GetType());
            }
        }

        return true;
    }

    public void LayEgg(Entity<CorticalBorerComponent> ent)
    {
        if (ent.Comp.Host is not { } host)
            return;

        if (ent.Comp.EggProto is not {} egg)
            return;

        var coordinates = _transform.ToMapCoordinates(host.ToCoordinates());
        Spawn(egg, coordinates);

        // TODO: Brain damage
        _damage.TryChangeDamage(host, new DamageSpecifier(_proto.Index<DamageGroupPrototype>("Brute"), 15), true, origin: ent, targetPart: TargetBodyPart.Head);
    }
}

public sealed class InfestHostAttempt : CancellableEntityEventArgs
{
    /// <summary>
    ///     The equipment that is blocking the entrance
    /// </summary>
    public EntityUid? Blocker = null;
}

[Serializable, NetSerializable]
public enum CorticalBorerDispenserUiKey
{
    Key,
}


[Serializable, NetSerializable]
public sealed class CorticalBorerDispenserSetInjectAmountMessage : BoundUserInterfaceMessage
{
    public readonly int CorticalBorerDispenserDispenseAmount;

    public CorticalBorerDispenserSetInjectAmountMessage(int amount)
    {
        CorticalBorerDispenserDispenseAmount = amount;
    }

    public CorticalBorerDispenserSetInjectAmountMessage(string s)
    {
        CorticalBorerDispenserDispenseAmount = s switch
        {
            "1" => 1,
            "5" => 5,
            "10" => 10,
            "15" => 15,
            "20" => 20,
            "25" => 25,
            "30" => 30,
            "50" => 50,
            "100" => 100,
            _ => throw new Exception($"Cannot convert the string `{s}` into a valid DispenseAmount"),
        };
    }
}

[Serializable, NetSerializable]
public sealed class CorticalBorerDispenserInjectMessage : BoundUserInterfaceMessage
{
    public readonly string ChemProtoId;

    public CorticalBorerDispenserInjectMessage(string proto)
    {
        ChemProtoId = proto;
    }
}

[Serializable, NetSerializable]
public sealed class CorticalBorerDispenserBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly List<CorticalBorerDispenserItem> DisList;

    public readonly int SelectedDispenseAmount;
    public CorticalBorerDispenserBoundUserInterfaceState(List<CorticalBorerDispenserItem> disList, int dispenseAmount)
    {
        DisList = disList;
        SelectedDispenseAmount = dispenseAmount;
    }
}

[Serializable, NetSerializable]
public sealed class CorticalBorerDispenserItem(string reagentName, string reagentId, int cost, int amount, int chems, Color reagentColor)
{
    public string ReagentName = reagentName;
    public string ReagentId = reagentId;
    public int Cost = cost;
    public int Amount = amount;
    public int Chems = chems;
    public Color ReagentColor = reagentColor;
}
