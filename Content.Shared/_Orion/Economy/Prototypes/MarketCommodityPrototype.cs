using Robust.Shared.Prototypes;

namespace Content.Shared._Orion.Economy.Prototypes;

[Prototype("marketCommodity")]
public sealed class MarketCommodityPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string Material = default!;

    [DataField]
    public float HighDemandMultiplier = 1.8f;

    [DataField]
    public float LowDemandMultiplier = 0.8f;
}
