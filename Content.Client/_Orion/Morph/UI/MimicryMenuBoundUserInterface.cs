using Content.Shared._Orion.Morph;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;

namespace Content.Client._Orion.Morph.UI;

//
// License-Identifier: AGPL-3.0-or-later
//

public sealed partial class MimicryMenuBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IClyde _displayManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;

    [NonSerialized] private MimicryMenu? _menu;

    public MimicryMenuBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_menu != null)
            {
                _menu.SendActivateMessageAction -= SendMessage;
            }
        }
        base.Dispose(disposing);
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<MimicryMenu>();
        _menu.SetEntity(Owner);
        _menu.SendActivateMessageAction += SendMessage;
        _menu.OpenCenteredAt(_inputManager.MouseScreenPosition.Position / _displayManager.ScreenSize);
    }

    private void SendMessage(NetEntity netEntity)
    {
        base.SendMessage(new EventMimicryActivate { Target = netEntity });
    }
}
