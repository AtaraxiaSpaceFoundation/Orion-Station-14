using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client._Orion.CorticalBorer;

public sealed class CorticalBorerWillingHostWindow : DefaultWindow
{
    public readonly Button AcceptButton;
    public readonly Button DenyButton;

    public CorticalBorerWillingHostWindow()
    {
        Title = Loc.GetString("cortical-borer-willing-title");

        Contents.AddChild(new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            Children =
            {
                new Label { Text = Loc.GetString("cortical-borer-willing-question") },
                new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
                    Align = AlignMode.Center,
                    Children =
                    {
                        (AcceptButton = new Button { Text = Loc.GetString("cortical-borer-willing-yes") }),
                        new Control { MinSize = new Vector2(16, 0) },
                        (DenyButton = new Button { Text = Loc.GetString("cortical-borer-willing-no") }),
                    }
                }
            }
        });
    }
}
