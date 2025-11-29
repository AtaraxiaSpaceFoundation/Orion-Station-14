using Content.Shared._Orion.Xenomorphs.Tail;
using Content.Shared._White.Xenomorphs.Xenomorph;
using Content.Shared.Actions;
using Content.Shared.Standing;
using Robust.Shared.Audio.Systems;

namespace Content.Server._Orion.Xenomorphs.Tail;

//
// License-Identifier: AGPL-3.0-or-later
//

public sealed class TailLashSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TailLashComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<TailLashComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<TailLashComponent, TailLashActionEvent>(OnLash);
    }

    private void OnComponentInit(EntityUid uid, TailLashComponent component, ComponentInit args)
    {
        _actions.AddAction(uid, ref component.TailLashAction, component.TailLashActionId, uid);
    }

    private void OnComponentShutdown(EntityUid uid, TailLashComponent component, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, component.TailLashAction);
    }

    private void OnLash(EntityUid uid, TailLashComponent component, TailLashActionEvent args)
    {
        var userXform = Transform(uid);
        var userPos = _transform.GetWorldPosition(userXform);

        var targets = new List<EntityUid>();
        foreach (var entity in _lookup.GetEntitiesInRange(uid, component.LashRange))
        {
            if (entity == uid)
                continue;

            if (HasComp<XenomorphComponent>(entity))
                continue;

            if (HasComp<StandingStateComponent>(entity))
            {
                _standing.Down(entity);
                targets.Add(entity);
            }
        }

        foreach (var target in targets)
        {
            if (!TryComp<TransformComponent>(target, out var targetXform))
                continue;

            var targetPos = _transform.GetWorldPosition(targetXform);
            var direction = userPos - targetPos;

            var angle = Angle.FromWorldVec(-direction) + Angle.FromDegrees(-90);

            var spawnPos = targetXform.Coordinates;
            var entity = Spawn(component.TailAnimationId, spawnPos);

            Transform(entity).LocalRotation = angle;
        }

        _audio.PlayPredicted(component.LashSound, uid, uid);
        args.Handled = true;
    }
}
