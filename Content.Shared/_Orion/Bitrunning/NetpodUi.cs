using Robust.Shared.Serialization;

namespace Content.Shared._Orion.Bitrunning;

[Serializable, NetSerializable]
public enum NetpodUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class NetpodOutfitEntry(string id, string name)
{
    public string Id = id;
    public string Name = name;
}

[Serializable, NetSerializable]
public sealed class NetpodBoundUiState(string? selectedOutfit, List<NetpodOutfitEntry> outfits) : BoundUserInterfaceState
{
    public string? SelectedOutfit = selectedOutfit;
    public List<NetpodOutfitEntry> Outfits = outfits;
}

[Serializable, NetSerializable]
public sealed class NetpodSelectOutfitMessage(string outfitId) : BoundUserInterfaceMessage
{
    public string OutfitId = outfitId;
}
