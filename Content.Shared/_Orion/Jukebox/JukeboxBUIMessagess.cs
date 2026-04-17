using Robust.Shared.Serialization;

namespace Content.Shared.White.Jukebox;

[Serializable, NetSerializable]
public class JukeboxStopRequest : BoundUserInterfaceMessage { }


[Serializable, NetSerializable]
public class JukeboxRepeatToggled : BoundUserInterfaceMessage
{
    public bool NewState { get; }
    public JukeboxRepeatToggled(bool newState)
    {
        NewState = newState;
    }
}

[Serializable, NetSerializable]
public class JukeboxEjectRequest : BoundUserInterfaceMessage { }
