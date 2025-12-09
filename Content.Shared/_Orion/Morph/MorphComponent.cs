using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Actions;
using Content.Shared.Damage;
using Robust.Shared.Containers;
using Content.Shared.Whitelist;
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

    [DataField]
    public DamageSpecifier DamageOnTouch = default!;

    [DataField]
    public string MorphSpawnProto = "MobMorph";

    [DataField]
    public float EatWeaponChanceOnHit = 0.2f;

    [DataField]
    public float EatWeaponChanceOnHited = 0.5f;

    [DataField]
    public int EatWeaponHungerReq = 5;

    [DataField]
    public int DetectableCount = 3;

    [DataField]
    public int OpenVentFoodReq = 5;

    [DataField]
    public int ReplicationFoodReq = 200;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public SoundSpecifier? SoundDevour = new SoundPathSpecifier("/Audio/Effects/demon_consume.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public SoundSpecifier? SoundReplication = new SoundPathSpecifier("/Audio/Announcements/outbreak7.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    [DataField]
    public float DevourTime = 3f;

    public List<EntityUid> ContainedCreatures = new();

    [AutoNetworkedField]
    public List<EntityUid> MemoryObjects = new();

    [DataField]
    public EntityWhitelist? DevourWhitelist = new();

    [DataField]
    public EntityWhitelist? DevourBlacklist = new();

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? DevourAction = "ActionMorphDevour";
    public EntityUid? DevourActionEntity;

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

    // public List<HumanoidAppearanceComponent> ApperanceList = new();
    //нужен для работы мимикрии под гуманойдов, больше ничего
    //бла-бла-бла, это надо если хотите делать морф под гуманоидов не костылями
    // public (EntityUid, HumanoidAppearanceComponent) NullspacedHumanoid = default;

    public float AmbushBlockRange = 2.15f;
}

public sealed partial class MorphMimicryRememberActionEvent : EntityTargetActionEvent;

public sealed partial class MorphReproduceActionEvent : InstantActionEvent;

public sealed partial class MorphOpenRadialMenuEvent : InstantActionEvent;

public sealed partial class MorphDevourActionEvent : EntityTargetActionEvent;

public sealed partial class MorphAmbushActionEvent : InstantActionEvent;

public sealed partial class MorphVentOpenActionEvent : EntityTargetActionEvent;

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
