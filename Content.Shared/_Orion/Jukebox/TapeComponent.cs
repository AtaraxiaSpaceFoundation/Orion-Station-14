using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.White.Jukebox;

[RegisterComponent, NetworkedComponent]
public sealed class TapeComponent : Component
{
    [DataField("songs")]
    public List<JukeboxSong> Songs { get; set; } = new();
}

[Serializable, NetSerializable]
public sealed class TapeComponentState : ComponentState
{
    public List<JukeboxSong> Songs { get; set; } = new();
}
