using Content.Client._Europa.CustomGhost.UI;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Europa.UserInterface.Buttons;

//
// License-Identifier: AGPL-3.0-or-later
//

public sealed class CustomGhostsMenuOpenButton : Button
{
    WindowTracker<CustomGhostsWindow> _customGhostWindow = new();
    public CustomGhostsMenuOpenButton() : base()
    {
        OnPressed += Pressed;
    }

    private void Pressed(ButtonEventArgs args)
    {
        _customGhostWindow.TryOpenCenteredLeft();
    }
}

