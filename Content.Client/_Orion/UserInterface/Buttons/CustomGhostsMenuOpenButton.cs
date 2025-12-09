using Content.Client._Orion.CustomGhost.UI;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Orion.UserInterface.Buttons;

//
// License-Identifier: AGPL-3.0-or-later
//

public sealed class CustomGhostsMenuOpenButton : Button
{
    private WindowTracker<CustomGhostsWindow> _customGhostWindow = new();

    public CustomGhostsMenuOpenButton()
    {
        OnPressed += Pressed;
    }

    private new void Pressed(ButtonEventArgs args)
    {
        _customGhostWindow.TryOpenCenteredLeft();
    }
}

