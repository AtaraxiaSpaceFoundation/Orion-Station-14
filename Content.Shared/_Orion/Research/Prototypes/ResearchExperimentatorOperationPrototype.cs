using Content.Shared.Damage;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared._Orion.Research.Prototypes;

[Prototype("researchExperimentatorOperation")]
public sealed partial class ResearchExperimentatorOperationPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    [DataField] public List<ProtoId<TagPrototype>> RequiredTags = new();
    [DataField] public List<ResearchPointAmount> SuccessReward = new();
    [DataField] public List<ResearchPointAmount> FailureReward = new();
    [DataField] public string? SuccessExperimentAction;
    [DataField] public string? FailureExperimentAction;
    [DataField] public string? BackfireExperimentAction;
    [DataField] public string? DiscoveryTrigger;
    [DataField] public float SuccessChance = 0.6f;
    [DataField] public float BackfireChanceOnFailure = 0.35f;
    [DataField] public DamageSpecifier BackfireDamage = new();
}
