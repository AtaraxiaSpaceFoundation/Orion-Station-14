using Robust.Shared.Prototypes;

namespace Content.Shared._Orion.Construction.Prototypes;

[Prototype("machinePart")]
public sealed partial class MachinePartPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string Name = string.Empty;

    [DataField(required: true)]
    public EntProtoId StockPartPrototype = string.Empty;
}
