using Content.Shared._Orion.Mood;
using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared._Orion.EntityEffects.Effects;

/// <summary>
///     Removes all non-categorized moodlets from an entity(anything not "Static" like hunger & thirst).
/// </summary>
[UsedImplicitly]
public sealed partial class ChemPurgeMoodlets : EntityEffect
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("reagent-effect-guidebook-purge-moodlets");

    [DataField]
    public bool RemovePermanentMoodlets;

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (args is not EntityEffectReagentArgs _)
            return;

        var entityManager = IoCManager.Resolve<EntityManager>();
        entityManager.EventBus.RaiseLocalEvent(args.TargetEntity, new MoodPurgeEffectsEvent(RemovePermanentMoodlets));
    }
}
