using System.Linq;
using Lidgren.Network;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.White.Jukebox;

[Serializable, NetSerializable]
public enum JukeboxUIKey : byte
{
    Key
}

[Serializable, NetSerializable]
public enum TapeCreatorUIKey : byte
{
    Key
}

[NetworkedComponent, RegisterComponent]
public class JukeboxComponent : Component
{

    public static string JukeboxContainerName = "jukebox_tapes";
    public static string JukeboxDefaultSongsName = "jukebox_default_tapes";

    [ViewVariables(VVAccess.ReadOnly)]
    public Container TapeContainer = default!;

    [DataField("defaultTapes")]
    public List<string> DefaultTapes = new();
    [ViewVariables(VVAccess.ReadOnly)]
    public Container DefaultSongsContainer = default!;


    [ViewVariables(VVAccess.ReadOnly)]
    public bool Repeating { get; set; } = true;

    [ViewVariables(VVAccess.ReadOnly)]
    public float Volume { get; set; }

    public PlayingSongData? PlayingSongData { get; set; }
}

public class TapeContainerComponent : Component
{
    public int MaxTapeCount = 1;
    public Container TapeContainer { get; set; } = new();
}

[Serializable, NetSerializable]
public class PlayingSongData
{
    public ResPath? SongPath;
    public string? SongName;
    public float PlaybackPosition;
    public float ActualSongLengthSeconds;
}

[Serializable, NetSerializable]
public class JukeboxComponentState : ComponentState
{
    public bool Playing { get; set; }

    public PlayingSongData? SongData { get; set; }
    public float Volume { get; set; }
}

[Serializable, NetSerializable, DataDefinition]
public class JukeboxSong
{
    [DataField("songName")]
    public string? SongName;
    [DataField("path")]
    public ResPath? SongPath;
}

[Serializable, NetSerializable]
public class JukeboxRequestSongPlay : EntityEventArgs
{
    public string? SongName { get; set; }
    public ResPath? SongPath { get; set; }
    public EntityUid? Jukebox { get; set; }
    public float SongDuration { get; set; }
}

[Serializable, NetSerializable]
public class JukeboxRequestStop : EntityEventArgs
{
    public EntityUid? JukeboxUid { get; set; }
}

[Serializable, NetSerializable]
public class JukeboxStopPlaying : EntityEventArgs
{
    public EntityUid? JukeboxUid { get; set; }
}

[Serializable, NetSerializable]
public class JukeboxSongUploadRequest : EntityEventArgs
{
    public string SongName = string.Empty;
    public List<byte> SongBytes = new();
    public EntityUid TapeCreatorUid = default!;
}

public class JukeboxSongUploadNetMessage : NetMessage
{
    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableUnordered;
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public ResPath RelativePath { get; set; } = ResPath.Self;
    public byte[] Data { get; set; } = Array.Empty<byte>();

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        var dataLength = buffer.ReadVariableInt32();
        Data = buffer.ReadBytes(dataLength);
        RelativePath = new ResPath(buffer.ReadString());
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.WriteVariableInt32(Data.Length);
        buffer.Write(Data);
        buffer.Write(RelativePath.ToString());
        buffer.Write(ResPath.Separator);
    }
}
