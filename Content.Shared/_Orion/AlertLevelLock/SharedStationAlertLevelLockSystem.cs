using System.Linq;
using Content.Shared._Orion.AlertLevelLock.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Lock;
using Content.Shared.Popups;

namespace Content.Shared._Orion.AlertLevelLock;

//
// License-Identifier: AGPL-3.0-or-later
//

public sealed class SharedStationAlertLevelLockSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;

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

        // Check if user has bypass access
        if (ent.Comp.BypassAccessLists.Count > 0)
        {
            // Get user's access tags
            var accessSources = _accessReader.FindPotentialAccessItems(args.User);
            var userAccessTags = _accessReader.FindAccessTags(args.User, accessSources);
            
            // Check if user has any of the required access groups
            foreach (var bypassList in ent.Comp.BypassAccessLists)
            {
                // Check if all required access tags in this group are present
                var hasAllAccess = bypassList.All(requiredTag => userAccessTags.Contains(requiredTag));
                
                if (hasAllAccess)
                {
                    // User has bypass access, allow opening
                    return;
                }
            }
        }

        // User doesn't have bypass access, deny
        _popup.PopupClient(
            Loc.GetString("access-failed-wrong-station-alert-level"),
            ent.Owner,
            args.User
        );

        args.Cancelled = true;
    }
}
