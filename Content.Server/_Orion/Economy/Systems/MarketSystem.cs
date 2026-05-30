using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server._Orion.Economy.Components;
using Content.Server.Station.Events;
using Content.Shared.Materials;
using Robust.Shared.Prototypes;

namespace Content.Server._Orion.Economy.Systems;

public sealed class MarketSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<StationPostInitEvent>(OnStationPostInit);
    }

    private void OnStationPostInit(ref StationPostInitEvent args)
    {
        if (!HasComp<StationMarketComponent>(args.Station))
            EnsureComp<StationMarketComponent>(args.Station);
    }

    public double AdjustSellPrice(EntityUid stationUid, EntityUid soldEntity, double basePrice)
    {
        if (!TryComp<StationMarketComponent>(stationUid, out var market) || !TryComp<PhysicalCompositionComponent>(soldEntity, out var composition))
            return basePrice;

        var totalWeight = composition.MaterialComposition.Values.Sum();
        if (totalWeight <= 0f)
            return basePrice;

        var weightedMultiplier = 0f;
        foreach (var (material, amount) in composition.MaterialComposition)
        {
            var multiplier = market.MaterialMultipliers.GetValueOrDefault(material, 1f);
            weightedMultiplier += multiplier * amount;
        }

        weightedMultiplier /= totalWeight;
        return basePrice * Math.Max(0.2f, weightedMultiplier);
    }

    public void SetMarketModifiers(EntityUid stationUid, Dictionary<string, float> modifiers)
    {
        var market = EnsureComp<StationMarketComponent>(stationUid);
        market.MaterialMultipliers.Clear();

        foreach (var (material, multiplier) in modifiers)
        {
            market.MaterialMultipliers[material] = multiplier;
            market.RecentChanges.Add(new MarketChangeSnapshot(material, multiplier, ++market.ChangeSequence));
        }

        if (market.RecentChanges.Count > market.MaxRecentChanges)
            market.RecentChanges.RemoveRange(0, market.RecentChanges.Count - market.MaxRecentChanges);
    }

    public void ClearMarketModifiers(EntityUid stationUid)
    {
        if (!TryComp<StationMarketComponent>(stationUid, out var market))
            return;

        market.MaterialMultipliers.Clear();
    }

    public Dictionary<string, float> GetActiveMarketModifiers(EntityUid stationUid)
    {
        return !TryComp<StationMarketComponent>(stationUid, out var market)
            ? new Dictionary<string, float>()
            : new Dictionary<string, float>(market.MaterialMultipliers);
    }

    public List<MarketChangeSnapshot> GetRecentMarketChanges(EntityUid stationUid)
    {
        return !TryComp<StationMarketComponent>(stationUid, out var market)
            ? new()
            : new List<MarketChangeSnapshot>(market.RecentChanges);
    }

    public void AnnounceMarketChanges(EntityUid stationUid, IReadOnlyList<string> increased, IReadOnlyList<string> decreased)
    {
        _chat.DispatchStationAnnouncement(stationUid,
            Loc.GetString("economy-report-market-changes",
                ("increased", increased.Count > 0 ? string.Join(", ", increased.Select(LocalizeMaterial)) : Loc.GetString("economy-report-none")),
                ("decreased", decreased.Count > 0 ? string.Join(", ", decreased.Select(LocalizeMaterial)) : Loc.GetString("economy-report-none"))),
            Loc.GetString("economy-report-sender"));
    }

    private string LocalizeMaterial(string materialId)
    {
        if (_proto.TryIndex<MaterialPrototype>(materialId, out var material) && !string.IsNullOrWhiteSpace(material.Name))
            return Loc.GetString(material.Name);

        return materialId;
    }
}
