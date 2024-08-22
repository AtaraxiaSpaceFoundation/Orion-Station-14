using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client._Europa.Lighting.Shaders;

public sealed class LightingOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    private LightingOverlay _lightingOverlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _lightingOverlay = new LightingOverlay(EntityManager, _prototypeManager);
        _overlayManager.AddOverlay(_lightingOverlay);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlayManager.RemoveOverlay(_lightingOverlay);
    }
}
