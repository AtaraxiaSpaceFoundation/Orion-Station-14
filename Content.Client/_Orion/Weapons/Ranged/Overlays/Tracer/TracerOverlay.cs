using Content.Client._Orion.Weapons.Ranged.Overlays.Systems;
using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client._Orion.Weapons.Ranged.Overlays.Tracer;

public sealed class TracerOverlay : Overlay
{
    private readonly TracerSystem _tracer;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;

    public TracerOverlay(TracerSystem tracer)
    {
        _tracer = tracer;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        _tracer.Draw(args.WorldHandle, args.MapId);
    }
}
