using Content.Shared._Orion.Mood;
using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared._Orion.EntityEffects.Effects;

/// <summary>
///     Adds a moodlet to an entity.
/// </summary>
[UsedImplicitly]
public sealed partial class ChemAddMoodlet : EntityEffect
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var moodPrototype = prototype.Index<MoodEffectPrototype>(MoodPrototype.Id);
        return Loc.GetString("reagent-effect-guidebook-add-moodlet",
            ("amount", moodPrototype.MoodChange),
            ("timeout", moodPrototype.Timeout));
    }

    /// <summary>
    ///     The mood prototype to be applied to the using entity.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<MoodEffectPrototype> MoodPrototype;

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (args is not EntityEffectReagentArgs _)
            return;

        var entityManager = IoCManager.Resolve<EntityManager>();
        var ev = new MoodEffectEvent(MoodPrototype);
        entityManager.EventBus.RaiseLocalEvent(args.TargetEntity, ev);
    }
}
