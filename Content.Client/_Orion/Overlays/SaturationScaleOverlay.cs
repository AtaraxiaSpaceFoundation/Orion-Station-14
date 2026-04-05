using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared._Orion.Overlays;

namespace Content.Client._Orion.Overlays;

public sealed class SaturationScaleOverlay : Overlay
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    private readonly ShaderInstance _shader;
    private float _currentSaturation = 1f;

    private static readonly ProtoId<ShaderPrototype> SaturationShaderId = "SaturationScale";

    public SaturationScaleOverlay()
    {
        IoCManager.InjectDependencies(this);

        _shader = _prototypeManager.Index(SaturationShaderId).Instance().Duplicate();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (_playerManager.LocalEntity is not { Valid: true } player
            || !_entityManager.HasComponent<SaturationScaleOverlayComponent>(player))
            return false;

        return base.BeforeDraw(in args);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture is null || _playerManager.LocalEntity is not { Valid: true } player
            || !_entityManager.HasComponent<SaturationScaleOverlayComponent>(player))
            return;

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("saturation", _currentSaturation);

        var handle = args.WorldHandle;
        handle.SetTransform(Matrix3x2.Identity);
        handle.UseShader(_shader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        if (ScreenTexture is null || _playerManager.LocalEntity is not { Valid: true } player
            || !_entityManager.TryGetComponent(player, out SaturationScaleOverlayComponent? saturationComp)
            || MathHelper.CloseTo(_currentSaturation, saturationComp.SaturationScale))
            return;

        var fadeInMultiplier = Math.Max(saturationComp.FadeInMultiplier, 1e-6f);
        var deltaTSlower = args.DeltaSeconds * fadeInMultiplier;
        var target = saturationComp.SaturationScale;
        _currentSaturation = MathHelper.CloseTo(_currentSaturation, target, deltaTSlower)
            ? target
            : MathHelper.Lerp(_currentSaturation, target, Math.Clamp(deltaTSlower, 0f, 1f));
        _shader.SetParameter("saturation", _currentSaturation);
    }
}
