using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared._Orion.Radio;

[Serializable, NetSerializable]
public sealed class PlayRadioBarkEvent : EntityEventArgs
{
    public string Path { get; init; } = string.Empty;
    public AudioParams Params { get; init; } = AudioParams.Default;
    public NetEntity Source { get; init; }
}
