using Content.Client._Orion.Doors.Components;
using Content.Shared._Orion.Doors.Components;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client._Orion.Doors;

public sealed class PhantomAirtightSpriteSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PhantomAirtightParentComponent, ComponentInit>(OnPhantomInit);
        SubscribeLocalEvent<PhantomAirtightParentComponent, ComponentShutdown>(OnPhantomShutdown);
        SubscribeLocalEvent<PhantomAirtightHostComponent, AppearanceChangeEvent>(OnHostAppearanceChange);
    }

    private void OnPhantomInit(Entity<PhantomAirtightParentComponent> ent, ref ComponentInit args)
        => RegisterAndSync(ent);

    private void OnPhantomShutdown(Entity<PhantomAirtightParentComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.ParentUid is not { } netParent)
            return;
        if (!TryGetEntity(netParent, out var parentUid))
            return;
        if (TryComp<PhantomAirtightHostComponent>(parentUid, out var host))
            host.Phantoms.Remove(ent.Owner);
    }

    private void RegisterAndSync(Entity<PhantomAirtightParentComponent> ent)
    {
        if (ent.Comp.ParentUid is not { } netParent)
            return;
        if (!TryGetEntity(netParent, out var parentUid))
            return;

        var host = EnsureComp<PhantomAirtightHostComponent>(parentUid.Value);
        if (!host.Phantoms.Contains(ent.Owner))
            host.Phantoms.Add(ent.Owner);

        // Initial DrawDepth sync
        if (!TryComp<SpriteComponent>(parentUid.Value, out var parentSprite))
            return;
        if (!TryComp<SpriteComponent>(ent.Owner, out var phantomSprite))
            return;
        _sprite.SetDrawDepth((ent.Owner, phantomSprite), parentSprite.DrawDepth);
    }

    private void OnHostAppearanceChange(Entity<PhantomAirtightHostComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;
        foreach (var phantom in ent.Comp.Phantoms)
        {
            if (!TryComp<SpriteComponent>(phantom, out var phantomSprite))
                continue;
            _sprite.SetDrawDepth((phantom, phantomSprite), args.Sprite.DrawDepth);
        }
    }
}
