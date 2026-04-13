using System.Diagnostics.CodeAnalysis;
using Content.Shared.Paper;
using Content.Shared.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._Orion.ChameleonStamp;

public abstract class SharedChameleonStampSystem : EntitySystem
{
    [Dependency] protected readonly IPrototypeManager Proto = default!;

    private List<ProtoId<EntityPrototype>> _stampsPresets = new();

    protected void UpdatePresets()
    {
        _stampsPresets.Clear();

        var prototypes = Proto.EnumeratePrototypes<EntityPrototype>();

        foreach (var proto in prototypes)
        {
            if (proto.Abstract || proto.HideSpawnMenu || !proto.HasComponent<StampComponent>())
                continue;

            _stampsPresets.Add(proto);
        }
    }

    public bool ValidatePreset(EntProtoId preset, [NotNullWhen(true)] out EntityPrototype? presetPrototype, [NotNullWhen(true)] out StampComponent? presetStampComponent)
    {
        presetPrototype = null;
        presetStampComponent = null;

        if (!_stampsPresets.Contains(preset.Id)
            || !Proto.TryIndex<EntityPrototype>(preset, out presetPrototype)
            || !presetPrototype.TryGetComponent(out presetStampComponent))
            return false;

        return true;
    }

    public IReadOnlyList<ProtoId<EntityPrototype>> GetAllPresets()
    {
        return _stampsPresets;
    }
}
