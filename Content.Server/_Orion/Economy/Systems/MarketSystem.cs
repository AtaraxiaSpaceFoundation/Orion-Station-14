using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server._Orion.Economy.Components;
using Content.Shared._Orion.Economy.Prototypes;
using Content.Shared.Materials;
using Content.Shared.Station.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Orion.Economy.Systems;

public sealed class MarketSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        var stationQuery = EntityQueryEnumerator<StationDataComponent>();
        while (stationQuery.MoveNext(out var stationUid, out _))
        {
            EnsureComp<StationMarketComponent>(stationUid);
        }

        var query = EntityQueryEnumerator<StationMarketComponent>();
        while (query.MoveNext(out var stationUid, out var market))
        {
            if (_timing.CurTime >= market.NextMarketUpdate)
            {
                market.NextMarketUpdate = _timing.CurTime + market.MarketUpdateDelay;
                RegenerateDemand((stationUid, market));
            }

            if (_timing.CurTime < market.NextReportUpdate)
                continue;

            market.NextReportUpdate = _timing.CurTime + market.ReportDelay;
            SendStationEconomicReport((stationUid, market));
        }
    }

    public double AdjustSellPrice(EntityUid stationUid, EntityUid soldEntity, double basePrice)
    {
        if (!TryComp<StationMarketComponent>(stationUid, out var market) || !TryComp<PhysicalCompositionComponent>(soldEntity, out var composition))
            return basePrice;

        var multiplier = 1f;
        foreach (var (material, amount) in composition.MaterialComposition)
        {
            if (!market.MaterialMultipliers.TryGetValue(material, out var materialMultiplier))
                continue;

            multiplier += (materialMultiplier - 1f) * amount;
        }

        return basePrice * Math.Max(0.2f, multiplier);
    }

    private void RegenerateDemand(Entity<StationMarketComponent> ent)
    {
        ent.Comp.MaterialMultipliers.Clear();
        var commodities = _proto.EnumeratePrototypes<MarketCommodityPrototype>().ToList();
        if (commodities.Count == 0)
            return;

        var highCount = Math.Min(3, commodities.Count);
        var lowCount = Math.Min(2, Math.Max(0, commodities.Count - highCount));

        for (var i = 0; i < highCount; i++)
        {
            var pick = _random.PickAndTake(commodities);
            ent.Comp.MaterialMultipliers[pick.Material] = pick.HighDemandMultiplier;
        }

        for (var i = 0; i < lowCount; i++)
        {
            var pick = _random.PickAndTake(commodities);
            ent.Comp.MaterialMultipliers[pick.Material] = pick.LowDemandMultiplier;
        }

        Dirty(ent);
    }

    private void SendStationEconomicReport(Entity<StationMarketComponent> ent)
    {
        var boosted = ent.Comp.MaterialMultipliers.Where(p => p.Value > 1f).Select(p => p.Key).ToList();

        if (boosted.Count > 0)
        {
            var joined = string.Join(", ", boosted);
            _chat.DispatchStationAnnouncement(ent.Owner,
                Loc.GetString("economy-report-high-demand", ("materials", joined)),
                Loc.GetString("economy-report-sender"));
            return;
        }

        _chat.DispatchStationAnnouncement(ent.Owner,
            Loc.GetString("economy-report-crisis"),
            Loc.GetString("economy-report-sender"));
    }
}
