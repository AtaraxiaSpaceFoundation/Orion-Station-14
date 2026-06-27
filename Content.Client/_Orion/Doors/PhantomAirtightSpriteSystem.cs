using Content.Shared._Orion.Doors.Components;
using Robust.Client.GameObjects;

namespace Content.Client._Orion.Doors.Systems;

public sealed class PhantomAirtightSpriteSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PhantomAirtightParentComponent, ComponentInit>(OnInit);
    }

    private void OnInit(Entity<PhantomAirtightParentComponent> ent, ref ComponentInit args)
    {
        if (ent.Comp.ParentUid == null)
            return;
        if (!TryGetEntity(ent.Comp.ParentUid.Value, out var parentUid))
            return;
        if (!TryComp<SpriteComponent>(parentUid, out var parentSprite))
            return;
        if (!TryComp<SpriteComponent>(ent.Owner, out var phantomSprite))
            return;

        _sprite.SetDrawDepth((ent.Owner, phantomSprite), parentSprite.DrawDepth);
    }
}
