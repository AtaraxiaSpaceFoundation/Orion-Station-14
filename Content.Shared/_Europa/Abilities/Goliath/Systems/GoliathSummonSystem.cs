using System.Numerics;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared.Abilities.Goliath;

public sealed class GoliathSpawnSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<GoliathSummonBabyAction>(OnSummonAction);
    }

    private void OnSummonAction(GoliathSummonBabyAction args)
    {
        if (args.Handled)
            return;

        var summonCount = Math.Clamp(args.SummonCount, 1, 8);
        var performer = args.Performer;
        var xform = Transform(performer);
        var coords = xform.Coordinates;
        var mapUid = xform.MapUid;

        if (mapUid == null)
            return;

        for (var i = 0; i < summonCount; i++)
        {
            var offset = new Vector2(_random.NextFloat(-1f, 1f), _random.NextFloat(-1f, 1f));
            var spawnCoords = coords.Offset(offset);

            if (_net.IsServer)
                Spawn(args.EntityId, spawnCoords);
        }

        args.Handled = true;
    }
}
