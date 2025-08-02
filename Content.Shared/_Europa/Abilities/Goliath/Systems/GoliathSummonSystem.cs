using Robust.Shared.Network;

namespace Content.Shared.Abilities.Goliath;

public sealed class GoliathSpawnSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<GoliathSummonBabyAction>(OnSummonAction);
    }

    private void OnSummonAction(GoliathSummonBabyAction args)
    {
        if (args.Handled)
            return;

        var performer = args.Performer;
        var xform = Transform(performer);
        var coords = xform.Coordinates;
        var mapUid = xform.MapUid;

        if (mapUid == null)
            return;

        for (var i = 0; i < args.SummonCount; i++)
        {
            if (_net.IsServer)
                Spawn(args.EntityId, coords);
        }

        args.Handled = true;
    }
}
