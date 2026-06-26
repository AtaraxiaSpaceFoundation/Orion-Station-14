using Content.Shared.Item;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;

namespace Content.Shared._Orion.Items.OpenableHeldPrefix;

public sealed class OpenableHeldPrefixSystem : EntitySystem
{
    [Dependency] private readonly SharedItemSystem _item = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OpenableHeldPrefixComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<OpenableHeldPrefixComponent, OpenableOpenedEvent>(OnOpened);
        SubscribeLocalEvent<OpenableHeldPrefixComponent, OpenableClosedEvent>(OnClosed);
    }

    private void OnStartup(Entity<OpenableHeldPrefixComponent> ent, ref ComponentStartup args)
    {
        UpdateHeldPrefix(ent);
    }

    private void OnOpened(Entity<OpenableHeldPrefixComponent> ent, ref OpenableOpenedEvent args)
    {
        UpdateHeldPrefix(ent, true);
    }

    private void OnClosed(Entity<OpenableHeldPrefixComponent> ent, ref OpenableClosedEvent args)
    {
        UpdateHeldPrefix(ent, false);
    }

    private void UpdateHeldPrefix(Entity<OpenableHeldPrefixComponent> ent, bool? opened = null)
    {
        if (!TryComp<OpenableComponent>(ent, out var openable))
            return;

        var isOpen = opened ?? openable.Opened;
        var prefix = isOpen ? ent.Comp.OpenedPrefix : ent.Comp.ClosedPrefix;

        _item.SetHeldPrefix(ent.Owner, prefix);
    }
}
