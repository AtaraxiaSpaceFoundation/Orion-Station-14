using Robust.Shared.Prototypes;

namespace Content.Shared._Europa.Donate;

[Prototype("donateItem")]
public sealed class DonateItemPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("ckey", required: true)]
    public string Ckey { get; } = default!;

    [DataField("item", required: true)]
    public string Item { get; } = default!;

    [DataField("is_battery")]
    public bool IsBattery { get; } = false;
}
