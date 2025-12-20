using Robust.Shared.GameStates;

namespace Content.Shared._Orion.Ghosts;

[RegisterComponent, AutoGenerateComponentState, NetworkedComponent]
public sealed partial class IgnoreInventoryBlockComponent : Component
{
    /// <summary>
    ///      If true, the entity can interact with blocked inventory slots.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IgnoreBlock = true;

    /// <summary>
    ///     If true, the entity can see all items including those in hidden slots.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ShowAllItems = true;
}
