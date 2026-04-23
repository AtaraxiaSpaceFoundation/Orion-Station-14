using Robust.Shared.GameStates;

namespace Content.Shared._Orion.Bitrunning.Components;

/// <summary>
/// Marks a disk that allows bitrunning avatar to use the player selected character profile.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BitrunningProfileDiskComponent : Component;
