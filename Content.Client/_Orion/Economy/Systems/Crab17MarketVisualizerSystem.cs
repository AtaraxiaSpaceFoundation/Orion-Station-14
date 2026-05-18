using Content.Shared._Orion.Economy;
using Content.Shared._Orion.Economy.Components;
using Robust.Client.GameObjects;

namespace Content.Client._Orion.Economy.Systems;

public sealed class Crab17MarketVisualizerSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<Crab17MarketComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(Entity<Crab17MarketComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite is not { } sprite)
            return;

        if (!_appearance.TryGetData<int>(ent, Crab17Visuals.StartupStage, out var stage, args.Component))
            stage = 0;

        _sprite.LayerSetVisible((ent, sprite), "flaps", stage <= 0);
        _sprite.LayerSetVisible((ent, sprite), "hatch", stage <= 1);
        _sprite.LayerSetVisible((ent, sprite), "legs_retracted", stage <= 2);
        _sprite.LayerSetVisible((ent, sprite), "legs_extending", stage == 3);
        _sprite.LayerSetVisible((ent, sprite), "legs_extended", stage is >= 4 and <= 6);
        _sprite.LayerSetVisible((ent, sprite), "legs", stage >= 7);
        _sprite.LayerSetVisible((ent, sprite), "hologram", stage >= 3);
        _sprite.LayerSetVisible((ent, sprite), "holosign", stage >= 3);
        _sprite.LayerSetVisible((ent, sprite), "screen_lines", stage >= 5);
        _sprite.LayerSetVisible((ent, sprite), "screen", stage >= 6);
        _sprite.LayerSetVisible((ent, sprite), "text", stage >= 7);
    }
}
