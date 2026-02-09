using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.Chemistry.EntitySystems;

public sealed class SolutionRegenerationSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SolutionRegenerationComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SolutionRegenerationComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
    }

    private void OnMapInit(Entity<SolutionRegenerationComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextRegenTime = _timing.CurTime + ent.Comp.Duration;

        Dirty(ent);
    }

    // Workaround for https://github.com/space-wizards/space-station-14/pull/35314
    private void OnEntRemoved(Entity<SolutionRegenerationComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        // Make sure the removed entity was our contained solution and clear our cached reference
        if (args.Entity == ent.Comp.SolutionRef?.Owner)
            ent.Comp.SolutionRef = null;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SolutionRegenerationComponent, SolutionContainerManagerComponent>();
        while (query.MoveNext(out var uid, out var regen, out var manager))
        {
            if (_timing.CurTime < regen.NextRegenTime)
                continue;

/* // Orion-Edit: Moved down
            // timer ignores if its full, it's just a fixed cycle
            regen.NextRegenTime += regen.Duration;
            // Needs to be networked and dirtied so that the client can reroll it during prediction
            Dirty(uid, regen);
*/

            // Orion-Edit-Start
            Solution? solution;
            if (regen.SolutionRef != null && !TerminatingOrDeleted(regen.SolutionRef.Value.Owner))
            {
                solution = regen.SolutionRef.Value.Comp.Solution;
            }
            else if (!_solutionContainer.ResolveSolution((uid, manager),
                         regen.SolutionName,
                         ref regen.SolutionRef,
                         out solution))
            {
                continue;
            }
            // Orion-Edit-End

            var amount = FixedPoint2.Min(solution.AvailableVolume, regen.Generated.Volume);
            if (amount <= FixedPoint2.Zero)
            {
                // Orion-Start
                regen.NextRegenTime += regen.Duration;
                Dirty(uid, regen);
                // Orion-End
                continue;
            }

            // Orion-Start
            regen.NextRegenTime += regen.Duration;
            Dirty(uid, regen);
            // Orion-End

            // Don't bother cloning and splitting if adding the whole thing
            var generated = amount == regen.Generated.Volume
                ? regen.Generated
                : regen.Generated.Clone().SplitSolution(amount);

            // Orion-Edit-Start
            _solutionContainer.TryAddSolution(regen.SolutionRef.Value, generated);
            regen.NextRegenTime += regen.Duration;
            Dirty(uid, regen);
            // Orion-Edit-End
        }
    }
}
