using Content.Server._Orion.Research.Systems;

namespace Content.Server._Orion.Research.Components;

[RegisterComponent, Access(typeof(ResearchDataDiskSystem))]
public sealed partial class ResearchDataDiskComponent : Component
{
    [DataField]
    public bool HasDataSnapshot;

    [DataField]
    public List<string> StoredTechnologies = new();

    [DataField]
    public string? SnapshotServerName;
}
