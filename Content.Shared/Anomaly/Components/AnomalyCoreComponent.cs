// SPDX-FileCopyrightText: 2023 Ed <96445749+TheShuEd@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: MIT

using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Anomaly.Components;

/// <summary>
/// This component exists for a limited time, and after it expires it modifies the entity, greatly reducing its value and changing its visuals
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedAnomalyCoreSystem), typeof(SharedAnomalySystem))] // Orion-Edit: Added SharedAnomalySystem
[AutoGenerateComponentState]
public sealed partial class AnomalyCoreComponent : Component
{
    /// <summary>
    /// Amount of time required for the core to decompose into an inert core
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public double TimeToDecay = 600;

    /// <summary>
    /// The moment of core decay. It is set during entity initialization.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public TimeSpan DecayMoment;

    /// <summary>
    /// The starting value of the entity.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public double StartPrice = 10000;

    /// <summary>
    /// The value of the object sought during decaying
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public double EndPrice = 200;

    /// <summary>
    /// Has the core decayed?
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public bool IsDecayed;

    /// <summary>
    /// The amount of GORILLA charges the core has.
    /// Not used when <see cref="IsDecayed"/> is false.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public int Charge = 5;

    // Orion-Start
    /// <summary>
    /// Prototype used when this core becomes inert.
    /// </summary>
    [DataField]
    public EntProtoId? DecayPrototype;

    /// <summary>
    /// Prototype used when this inert core is successfully reactivated.
    /// </summary>
    [DataField]
    public EntProtoId? ReactivationPrototype;

    /// <summary>
    /// Anomaly prototype that can be spawned as a hazardous side effect.
    /// </summary>
    [DataField]
    public EntProtoId? HazardousAnomalyPrototype;

    /// <summary>
    /// Reagents that can reactivate an inert anomaly core.
    /// </summary>
    [DataField]
    public List<ProtoId<ReagentPrototype>> ReactivationReagents = [];

    /// <summary>
    /// Reagents that may cause dangerous side effects during reactivation attempts.
    /// </summary>
    [DataField]
    public List<ProtoId<ReagentPrototype>> HazardousReactivationReagents = [];

    /// <summary>
    /// Amount of reagent consumed to reactivate a core.
    /// </summary>
    [DataField]
    public FixedPoint2 ReactivationReagentAmount = FixedPoint2.New(5);

    [DataField]
    public float HazardousFailureChance = 0.65f;
    // Orion-End
}
