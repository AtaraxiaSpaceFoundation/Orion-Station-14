using Content.Shared._Orion.ChameleonStamp;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client._Orion.ChameleonStamp;

public sealed partial class ChameleonStampSystem : SharedChameleonStampSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChameleonStampComponent, AfterAutoHandleStateEvent>(HandleState);
        UpdatePresets();
    }

    private void HandleState(Entity<ChameleonStampComponent> entity, ref AfterAutoHandleStateEvent args)
    {
        UpdateVisuals(entity);
    }

    public void UpdateVisuals(Entity<ChameleonStampComponent> entity)
    {
        if (!TryComp<SpriteComponent>(entity, out var sprite)
            || !Proto.TryIndex<EntityPrototype>(entity.Comp.SelectedStampSpritePrototype, out var proto)
            || !proto.TryGetComponent<SpriteComponent>(out var presetSprite))
            return;

        sprite.CopyFrom(presetSprite);
    }
}
