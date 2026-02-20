using Content.Shared._Orion.Morph;
using Content.Shared.Objectives.Components;

namespace Content.Server._Orion.Morph.Objectives;

public sealed class MorphDevourLivingConditionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MorphDevourLivingConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(Entity<MorphDevourLivingConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        if (!TryGetMorph(args.Mind.OwnedEntity, out var morph) || ent.Comp.Target <= 0)
        {
            args.Progress = 0f;
            return;
        }

        args.Progress = MathF.Min(1f, morph.LivingDevoured / (float) ent.Comp.Target);
    }

    private bool TryGetMorph(EntityUid? ownedEntity, out MorphComponent morph)
    {
        if (ownedEntity is not { } entity)
        {
            morph = default!;
            return false;
        }

        if (TryComp<MorphComponent>(entity, out var entityMorph))
        {
            morph = entityMorph;
            return true;
        }

        morph = default!;
        return false;
    }
}
