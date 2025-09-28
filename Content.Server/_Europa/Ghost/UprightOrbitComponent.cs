using Content.Shared.Follower;
using Content.Shared.Follower.Components;

namespace Content.Server._Europa.Ghost;

//
// License-Identifier: AGPL-3.0-or-later
//

[RegisterComponent]
public sealed partial class UprightOrbitComponent : Component;

public sealed class UprightOrbitSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<UprightOrbitComponent, StartedFollowingEntityEvent>(OnStartedFollowing);
    }

    private void OnStartedFollowing(EntityUid uid, UprightOrbitComponent comp, StartedFollowingEntityEvent args)
    {
        if (TryComp<OrbitVisualsComponent>(uid, out var orbit))
            orbit.KeepUpright = true;
    }
}
