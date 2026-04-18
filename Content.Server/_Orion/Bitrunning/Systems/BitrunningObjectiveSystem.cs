using Content.Shared._Orion.Bitrunning.Components;
using Content.Shared.Interaction;

namespace Content.Server._Orion.Bitrunning.Systems;

public sealed class BitrunningObjectiveSystem : EntitySystem
{
    [Dependency] private readonly QuantumServerSystem _server = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BitrunningObjectivePointComponent, InteractHandEvent>(OnInteract);
    }

    private void OnInteract(Entity<BitrunningObjectivePointComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        var mapUid = Transform(ent).MapUid;
        if (mapUid == null)
            return;

        var query = EntityQueryEnumerator<QuantumServerComponent>();
        while (query.MoveNext(out var serverUid, out var server))
        {
            if (server.DomainMapUid != mapUid)
                continue;

            _server.AddObjectivePoint(serverUid, ent.Comp.Points);
            if (ent.Comp.ConsumeOnUse)
                QueueDel(ent.Owner);

            args.Handled = true;
            return;
        }
    }
}
