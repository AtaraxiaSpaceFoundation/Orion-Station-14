using Content.Shared.Eui;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Orion.ReadyManifest;

[Serializable, NetSerializable]
public sealed class RequestReadyManifestMessage : EntityEventArgs;

[Serializable, NetSerializable]
public sealed class ReadyManifestEuiState : EuiStateBase
{
    public Dictionary<ProtoId<JobPrototype>, List<string>> JobCharacters { get; }

    public ReadyManifestEuiState(Dictionary<ProtoId<JobPrototype>, List<string>> jobCharacters)
    {
        JobCharacters = jobCharacters;
    }
}
