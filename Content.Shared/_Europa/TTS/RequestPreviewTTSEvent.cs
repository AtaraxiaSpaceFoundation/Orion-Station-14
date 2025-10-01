using Robust.Shared.Serialization;

namespace Content.Shared._Europa.TTS;

// ReSharper disable once InconsistentNaming
[Serializable, NetSerializable]
public sealed class RequestPreviewTTSEvent(string voiceId, string species) : EntityEventArgs
{
    public string VoiceId { get; } = voiceId;
    public string Species { get; } = species;
}
