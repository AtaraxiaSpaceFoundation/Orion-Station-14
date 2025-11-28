using Content.Shared._Orion.Xenomorphs.Tail;
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
        foreach (var entity in _lookup.GetEntitiesInRange(uid, component.LashRange))
        {
            if (HasComp<StandingStateComponent>(entity))
                _standing.Down(entity);
        }

        _audio.PlayPredicted(component.LashSound, uid, uid);
    }
}
