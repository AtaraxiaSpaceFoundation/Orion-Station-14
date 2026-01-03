using Content.Shared._Orion.ReadyManifest;

namespace Content.Client._Orion.ReadyManifest;

public sealed class ReadyManifestSystem : EntitySystem
{
    private readonly HashSet<string> _departments = [];

    public IReadOnlySet<string> Departments => _departments;

    public void RequestReadyManifest()
    {
        RaiseNetworkEvent(new RequestReadyManifestMessage());
    }
}
