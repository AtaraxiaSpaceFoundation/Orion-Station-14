using Content.Shared._Orion.CorticalBorer.Components;
using Content.Shared.Objectives.Components;

namespace Content.Server._Orion.CorticalBorer.Objectives;

public sealed class CorticalBorerEggsLaidConditionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CorticalBorerEggsLaidConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(Entity<CorticalBorerEggsLaidConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        if (!TryGetBorer(args.Mind.OwnedEntity, out var borer) || ent.Comp.Target <= 0)
        {
            args.Progress = 0f;
            return;
        }

        args.Progress = MathF.Min(1f, borer.EggsLaid / (float) ent.Comp.Target);
    }

    private bool TryGetBorer(EntityUid? ownedEntity, out CorticalBorerComponent borer)
    {
        if (ownedEntity is not { } entity)
        {
            borer = default!;
            return false;
        }

        if (TryComp<CorticalBorerComponent>(entity, out var entityBorer))
        {
            borer = entityBorer;
            return true;
        }

        if (TryComp<CorticalBorerInfestedComponent>(entity, out var infested) &&
            TryComp<CorticalBorerComponent>(infested.Borer, out var infestedBorer))
        {
            borer = infestedBorer;
            return true;
        }

        borer = default!;
        return false;
    }
}
