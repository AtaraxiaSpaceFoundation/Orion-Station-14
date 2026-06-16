using System.Collections.Generic;
using Robust.Shared.GameObjects;

namespace Content.Client._Orion.Doors.Components;

/// <summary>
/// Client-only marker added to the parent door to track its phantom airtight entities.
/// </summary>
[RegisterComponent]
public sealed partial class PhantomAirtightHostComponent : Component
{
    public readonly List<EntityUid> Phantoms = new();
}
