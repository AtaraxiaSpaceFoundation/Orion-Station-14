using Content.Client.UserInterface.Controls;
using Content.Shared._Orion.Morph;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using System.Numerics;

namespace Content.Client._Orion.Morph.UI;

//
// License-Identifier: AGPL-3.0-or-later
//

public sealed partial class MimicryMenu : RadialMenu
{
    [Dependency] private readonly EntityManager _ent = default!;

    public EntityUid Entity { get; private set; }

    public event Action<NetEntity>? SendActivateMessageAction;

    public MimicryMenu()
    {
        IoCManager.InjectDependencies(this);
        RobustXamlLoader.Load(this);
    }

    public void SetEntity(EntityUid ent)
    {
        Entity = ent;
        UpdateUI();
    }

    private void UpdateUI()
    {
        var main = FindControl<RadialContainer>("Main");

        main.RemoveAllChildren();

        if (!_ent.TryGetComponent<MorphComponent>(Entity, out var morph))
            return;

        main.RemoveAllChildren();

        foreach (var morphable in morph.MemoryObjects)
        {
            if (!_ent.TryGetComponent<MetaDataComponent>(morphable, out var md))
                continue;

            var button = new EmbeddedEntityMenuButton
            {
                SetSize = new Vector2(64, 64),
                ToolTip = md.EntityName,
                NetEntity = md.NetEntity,
            };

            var texture = new SpriteView(morphable, _ent)
            {
                OverrideDirection = Direction.South,
                VerticalAlignment = VAlignment.Center,
                SetSize = new Vector2(64, 64),
                VerticalExpand = true,
                Stretch = SpriteView.StretchMode.Fill,
            };
            button.AddChild(texture);

            main.AddChild(button);
        }
        AddAction(main);
    }

    private void AddAction(RadialContainer main)
    {
        foreach (var child in main.Children)
        {
            var castChild = child as EmbeddedEntityMenuButton;
            if (castChild == null)
                continue;

            castChild.OnButtonUp += _ =>
            {
                SendActivateMessageAction?.Invoke(castChild.NetEntity);
                Close();
            };
        }
    }

    public sealed class EmbeddedEntityMenuButton : RadialMenuTextureButtonWithSector
    {
        public NetEntity NetEntity;
    }
}
