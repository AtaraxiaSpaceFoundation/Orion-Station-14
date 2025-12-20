using Robust.Shared.GameStates;

namespace Content.Shared._Orion.Ghosts;

[RegisterComponent, AutoGenerateComponentState, NetworkedComponent]
public sealed partial class IgnoreInventoryBlockComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool IgnoreBlock = true;

    [DataField, AutoNetworkedField]
    public bool ShowAllItems = true;
}
