using Content.Goobstation.Maths.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Robust.Shared.Containers;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization;

namespace Content.Shared._Orion.Morph;

//
// License-Identifier: AGPL-3.0-or-later
//

[RegisterComponent, AutoGenerateComponentState, NetworkedComponent]
public sealed partial class MorphComponent : Component
{
    /// <summary>
    ///     Container for various consumable items.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public Container Container = default!;
    public string ContainerId = "morphContainer";

    /// <summary>
    ///     Just need.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public Container MimicryContainer = default!;
    public string MimicryContainerId = "mimicryContainer";

    [DataField, AutoNetworkedField]
    public FixedPoint2 Biomass;

    [DataField]
    public FixedPoint2 MaxBiomass = FixedPoint2.New(300);

    [DataField]
    public List<EntProtoId> Objectives = new()
    {
        "MorphSurviveObjective",
        "MorphDevourLivingObjective",
        "MorphReproduceObjective",
    };

    [DataField]
    public DamageSpecifier DamageOnTouch = default!;

    [DataField]
    public string MorphSpawnProto = "MobMorph";

    [DataField]
    public float DevourWeaponOnHit = 0.2f;

    [DataField]
    public float DevourWeaponOnBeingHit = 0.5f;

    [DataField]
    public int DevourWeaponHungerCost = -5;

    [DataField(required: true)]
    public DamageSpecifier? DevourHealingDamage;

    [DataField]
    public int DetectableCount = 3;

    [DataField]
    public int OpenVentCost = 5;

    [DataField]
    public int ReplicationCost = 200;

    [DataField]
    public SoundSpecifier? SoundDevour = new SoundPathSpecifier("/Audio/Effects/demon_consume.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    [DataField]
    public SoundSpecifier? SoundReplication = new SoundPathSpecifier("/Audio/Announcements/outbreak7.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    [AutoNetworkedField]
    public List<EntityUid> MemoryObjects = [];

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? MemoryAction = "ActionMorphRemember";
    public EntityUid? MemoryActionEntity;

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? ReplicationAction = "ActionMorphReplication";
    public EntityUid? ReplicationActionEntity;

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? MimicryAction = "ActionMorphMimicry";
    public EntityUid? MimicryActionEntity;

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? AmbushAction = "ActionMorphAmbush";
    public EntityUid? AmbushActionEntity;

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? VentOpenAction = "ActionMorphVentOpen";
    public EntityUid? VentOpenActionEntity;

    //  public List<HumanoidAppearanceComponent> ApperanceList = new();
    //  нужен для работы мимикрии под гуманойдов, больше ничего
    //  бла-бла-бла, это надо если хотите делать морф под гуманоидов не костылями
    //  public (EntityUid, HumanoidAppearanceComponent) NullspacedHumanoid = default;

    /// <summary>
    ///     Range for where ambush doesn't work around not morphs
    /// </summary>
    public float AmbushBlockRange = 2.15f;

    /// <summary>
    ///     Amount of morphs this specific morph has produced
    /// </summary>
    [DataField]
    public int Children;

    [DataField]
    public int TotalChildren;

    /// <summary>
    ///     Parent morph that spawned this morph.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? ParentMorph;

    /// <summary>
    ///     How much damage to trigger undisguise
    /// </summary>
    [DataField]
    public FixedPoint2 DamageThreshold = FixedPoint2.New(2);

    [DataField]
    public ProtoId<AlertPrototype> BiomassAlert = "Biomass";

    [DataField]
    public int RoundEndDevourTarget = 3;

    [DataField]
    public int RoundEndReproduceTarget = 2;

    [DataField]
    public int LivingDevoured;
}

[Serializable, NetSerializable]
public sealed class EventMimicryActivate : BoundUserInterfaceMessage
{
    public NetEntity? Target { get; set; }
}

[Serializable, NetSerializable]
public enum MimicryKey : byte
{
    Key,
}
