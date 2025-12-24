using Robust.Shared.GameStates;

namespace Content.Shared.Inventory;

/// <summary>
/// Used to prevent items from being unequipped and equipped from slots that are listed in <see cref="Slots"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState] // Orion-Edit: Removed access SlotBlockSystem
public sealed partial class SlotBlockComponent : Component
{
    // Orion-Start
    /// <summary>
    /// Slots that this entity should block and hide.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<SlotFlags> BlockList = new();

    /// <summary>
    /// Slots that this entity should only hide.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<SlotFlags> HideList = new();
    // Orion-End

    /// <summary>
    /// Slots that this entity should block.
    /// </summary>
    [DataField, AutoNetworkedField] // Orion-Edit: Removed required
    public SlotFlags Slots = SlotFlags.NONE;
}
