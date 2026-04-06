using Content.Shared._Orion.Mood;
using Content.Shared._Orion.Traits.Components;
using Robust.Shared.Random;

namespace Content.Shared._Orion.Traits.Systems;

public sealed class MoodTraitSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MoodTraitComponent, ComponentStartup>(OnMoodTraitStartup);
        SubscribeLocalEvent<ManicComponent, OnSetMoodEvent>(OnManicMood);
        SubscribeLocalEvent<MercurialComponent, OnSetMoodEvent>(OnMercurialMood);
        SubscribeLocalEvent<DeadEmotionsComponent, OnSetMoodEvent>(OnDeadEmotionsMood);
    }

    private void OnMoodTraitStartup(Entity<MoodTraitComponent> ent, ref ComponentStartup args)
    {
        foreach (var moodlet in ent.Comp.MoodEffects)
        {
            var ev = new MoodEffectEvent(moodlet);
            RaiseLocalEvent(ent.Owner, ev);
        }
    }

    private void OnManicMood(EntityUid uid, ManicComponent component, ref OnSetMoodEvent args)
    {
        var lower = MathF.Max(0f, component.LowerMultiplier);
        var upper = MathF.Max(0f, component.UpperMultiplier);

        if (lower > upper)
            (lower, upper) = (upper, lower);

        args.MoodChangedAmount *= _random.NextFloat(lower, upper);
    }

    private void OnMercurialMood(EntityUid uid, MercurialComponent component, ref OnSetMoodEvent args)
    {
        var lower = component.LowerMood;
        var upper = component.UpperMood;

        if (lower > upper)
            (lower, upper) = (upper, lower);

        args.MoodOffset += _random.NextFloat(lower, upper);
    }

    private static void OnDeadEmotionsMood(EntityUid uid, DeadEmotionsComponent component, ref OnSetMoodEvent args)
    {
        args.MoodChangedAmount = 0f;
        args.MoodOffset = 0f;
    }
}
