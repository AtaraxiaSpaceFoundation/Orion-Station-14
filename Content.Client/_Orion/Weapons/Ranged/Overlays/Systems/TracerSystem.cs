using System.Numerics;
using Content.Client._Orion.Weapons.Ranged.Overlays.Tracer;
using Content.Shared._Orion.Weapons.Ranged.Components;
using Robust.Client.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client._Orion.Weapons.Ranged.Overlays.Systems;

public sealed class TracerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private TracerOverlay? _tracerOverlay;

    public override void Initialize()
    {
        base.Initialize();
        _tracerOverlay = new TracerOverlay(this);
        _overlay.AddOverlay(_tracerOverlay);

        SubscribeLocalEvent<TracerComponent, ComponentStartup>(OnTracerStart);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        if (_tracerOverlay == null)
            return;

        _overlay.RemoveOverlay(_tracerOverlay);
        _tracerOverlay = null;
    }

    private void OnTracerStart(Entity<TracerComponent> ent, ref ComponentStartup args)
    {
        var xform = Transform(ent);
        var pos = _transform.GetWorldPosition(xform);

        ent.Comp.Data = new TracerData(
            new List<Vector2> { pos },
            _timing.CurTime + TimeSpan.FromSeconds(ent.Comp.Lifetime)
        );
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<TracerComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var tracer, out var xform))
        {
            var data = tracer.Data;

            if (curTime > data.EndTime)
            {
                RemCompDeferred<TracerComponent>(uid);
                continue;
            }

            var currentPos = _transform.GetWorldPosition(xform);
            data.PositionHistory.Add(currentPos);

            while (data.PositionHistory.Count > 2 && GetTrailLength(data.PositionHistory) > tracer.Length)
            {
                data.PositionHistory.RemoveAt(0);
            }
        }
    }

    private static float GetTrailLength(List<Vector2> positions)
    {
        var length = 0f;
        for (var i = 1; i < positions.Count; i++)
        {
            length += Vector2.Distance(positions[i - 1], positions[i]);
        }

        return length;
    }

    public void Draw(DrawingHandleWorld handle, MapId currentMap)
    {
        var query = EntityQueryEnumerator<TracerComponent, TransformComponent>();

        while (query.MoveNext(out _, out var tracer, out var xform))
        {
            if (xform.MapID != currentMap)
                continue;

            var data = tracer.Data;
            if (data.PositionHistory.Count < 2)
                continue;

            var positions = data.PositionHistory;

            handle.SetTransform(Matrix3x2.Identity);

            for (var i = 1; i < positions.Count; i++)
            {
                handle.DrawLine(positions[i - 1], positions[i], tracer.Color);
            }
        }
    }
}
