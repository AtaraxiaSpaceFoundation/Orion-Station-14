using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;

namespace Content.Shared._Orion.Bitrunning.Prototypes;

[Prototype("bitrunningVirtualDomain")]
public sealed class BitrunningVirtualDomainPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField(required: true)]
    public string Name { get; private set; } = string.Empty;

    [DataField(required: true)]
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Server points required to start this domain.
    /// </summary>
    [DataField]
    public int Cost { get; private set; }

    [DataField]
    public BitrunningDifficulty Difficulty { get; private set; } = BitrunningDifficulty.Easy;

    /// <summary>
    /// Base server points rewarded on successful completion.
    /// </summary>
    [DataField]
    public int RewardPoints { get; private set; } = 1;

    [DataField(required: true, customTypeSerializer: typeof(ResPathSerializer))]
    public ResPath MapPath { get; private set; }

    /// <summary>
    /// Marks this domain as modular in UI/data for future modular segment pipelines.
    /// </summary>
    [DataField]
    public bool IsModular { get; private set; }

    /// <summary>
    /// Indicates availability of secondary objectives/loot hooks for this domain.
    /// </summary>
    [DataField]
    public bool HasSecondaryObjectives { get; private set; }

    /// <summary>
    /// If true, domain info is redacted until scanner/points thresholds are met.
    /// </summary>
    [DataField]
    public bool HiddenUntilScanned { get; private set; } = true;

    /// <summary>
    /// Minimum scanner tier required to reveal non-redacted domain identity/details.
    /// </summary>
    [DataField]
    public int RequiredScannerTier { get; private set; } = 1;

    /// <summary>
    /// Minimum server points required to reveal reward value in UI.
    /// </summary>
    [DataField]
    public int RequiredPointsToRevealReward { get; private set; }

    /// <summary>
    /// Defines how the domain is completed.
    /// </summary>
    [DataField]
    public BitrunningObjectiveType ObjectiveType { get; private set; } = BitrunningObjectiveType.CollectEncryptedCaches;

    /// <summary>
    /// Objective progress needed to trigger reward flow.
    /// </summary>
    [DataField]
    public int ObjectiveTarget { get; private set; } = 10;

    /// <summary>
    /// Free-form semantic flags for domain behavior gates.
    /// </summary>
    [DataField]
    public string[] Flags { get; private set; } = [];

    /// <summary>
    /// Secondary loot prototype IDs available for reward pipeline extensions.
    /// </summary>
    [DataField]
    public string[] OptionalSecondaryLoot { get; private set; } = [];

    /// <summary>
    /// Optional ghost-role prototype IDs used by domain-specific ghost integration.
    /// </summary>
    [DataField]
    public string[] OptionalGhostRoles { get; private set; } = [];

    /// <summary>
    /// If true, avatar entities are deleted once a bitrunner disconnects from them.
    /// </summary>
    [DataField]
    public bool DeleteAvatarOnDisconnect { get; private set; }

    /// <summary>
    /// Optional forced loadout for this domain. Has priority over netpod selection.
    /// </summary>
    [DataField]
    public ProtoId<StartingGearPrototype>? ForcedLoadout { get; private set; }
}
