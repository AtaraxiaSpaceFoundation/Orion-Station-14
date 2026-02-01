using Robust.Shared.GameStates;

namespace Content.Shared._Orion.Morph;

[RegisterComponent, NetworkedComponent]
public sealed partial class MorphDisguiseComponent : Component
{
    public string ExamineMessage = $"[color=darkgreen]{Loc.GetString("morph-examined-strange")}[/color]";
}
