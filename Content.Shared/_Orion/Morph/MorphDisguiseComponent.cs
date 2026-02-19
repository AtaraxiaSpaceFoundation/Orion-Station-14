using Robust.Shared.GameStates;

namespace Content.Shared._Orion.Morph;

[RegisterComponent, NetworkedComponent]
public sealed partial class MorphDisguiseComponent : Component
{
    [DataField]
    public LocId ExamineMessage = "morph-examined-strange";

    [DataField]
    public Color ExamineColor = Color.DarkGreen;
}
