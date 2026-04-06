using Content.Shared._Orion.Sprite.EdgeConnections;
using Robust.Client.GameObjects;

namespace Content.Client._Orion.Sprite.EdgeConnections;

public sealed class EdgeConnectionVisualizerSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<EdgeConnectionComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(Entity<EdgeConnectionComponent> ent, ref AppearanceChangeEvent args)
    {
        _appearance.TryGetData(ent, EdgeConnectionVisuals.ConnectionMask, out EdgeConnectionDirections _, args.Component);
    }
}
