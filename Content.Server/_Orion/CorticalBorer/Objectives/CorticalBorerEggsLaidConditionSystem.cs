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
        if (args.Mind.OwnedEntity is not { } ownedEntity ||
            !TryComp<CorticalBorerComponent>(ownedEntity, out var borer) ||
            ent.Comp.Target <= 0)
            return;

        args.Progress = MathF.Min(1f, borer.EggsLaid / (float) ent.Comp.Target);
    }
}
