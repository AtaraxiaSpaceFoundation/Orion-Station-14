using Content.Shared._Orion.AlertLevelLock.Components;
using Content.Shared.Lock;
using Content.Shared.Popups;

namespace Content.Shared._Orion.AlertLevelLock;

//
// License-Identifier: AGPL-3.0-or-later
//

public sealed class SharedStationAlertLevelLockSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationAlertLevelLockComponent, LockToggleAttemptEvent>(OnTryAccess);
    }

    private void OnTryAccess(Entity<StationAlertLevelLockComponent> ent, ref LockToggleAttemptEvent args)
    {
        if (!TryComp<LockComponent>(ent.Owner, out var lockComponent))
            return;

        if (!ent.Comp.Enabled || !ent.Comp.Locked)
            return;

        // If the user is trying to lock the object (not unlock), allow it.
        var isLocking = !lockComponent.Locked;
        if (isLocking)
            return;

        _popup.PopupClient(
            Loc.GetString("access-failed-wrong-station-alert-level"),
            ent.Owner,
            args.User
        );

        args.Cancelled = true;
    }
}
