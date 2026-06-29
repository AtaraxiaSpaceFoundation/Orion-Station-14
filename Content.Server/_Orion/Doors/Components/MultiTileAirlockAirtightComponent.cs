namespace Content.Server._Orion.Doors.Components;

/// <summary>
/// Marks a 2-tile-wide airlock so that a phantom airtight entity is spawned
/// on the adjacent tile, properly sealing atmosphere on both tiles when closed.
/// The offset is derived from the entity's local rotation (sprite offset 0.5,0
/// means the second tile is always at +1 local X).
/// </summary>
[RegisterComponent]
public sealed partial class MultiTileAirlockAirtightComponent : Component
{
    /// <summary>
    /// Local-space offsets (pre-rotation) of additional tiles to seal.
    /// </summary>
    [DataField]
    public List<Vector2i> TileOffsets = new() { new Vector2i(1, 0) };

    [ViewVariables]
    public List<EntityUid> PhantomEntities = new();
}
