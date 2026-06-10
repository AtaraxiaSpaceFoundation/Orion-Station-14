using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Shared._Orion.Radio;

/// <summary>
/// Marker component on audio entities spawned by radio bark playback.
/// The client uses it to apply the per-player radio volume CVar.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RadioBarkAudioComponent : Component;
