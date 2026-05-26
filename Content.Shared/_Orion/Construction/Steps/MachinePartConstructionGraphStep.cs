using Content.Shared._Orion.Construction.Components;
using Content.Shared._Orion.Construction.Prototypes;
using Content.Shared.Construction;
using Content.Shared.Construction.Steps;
using Content.Shared.Examine;
using Robust.Shared.Prototypes;

namespace Content.Shared._Orion.Construction.Steps;

[DataDefinition]
public sealed partial class MachinePartConstructionGraphStep : ArbitraryInsertConstructionGraphStep
{
    [DataField(required: true)]
    public ProtoId<MachinePartPrototype> MachinePart;

    public override bool EntityValid(EntityUid uid, IEntityManager entityManager, IComponentFactory compFactory)
    {
        return entityManager.TryGetComponent(uid, out MachinePartComponent? machinePart) && machinePart.Part == MachinePart;
    }

    public override void DoExamine(ExaminedEvent examinedEvent)
    {
        var localizedPartName = MachinePart.Id;
        if (IoCManager.Resolve<IPrototypeManager>().TryIndex(MachinePart, out var machinePartProto) && !string.IsNullOrWhiteSpace(machinePartProto.Name))
            localizedPartName = Loc.GetString(machinePartProto.Name);

        examinedEvent.PushMarkup(string.IsNullOrEmpty(Name)
            ? Loc.GetString("construction-insert-entity-with-component", ("componentName", localizedPartName))
            : Loc.GetString("construction-insert-exact-entity", ("entityName", Loc.GetString(Name))));
    }

    public override ConstructionGuideEntry GenerateGuideEntry()
    {
        return new ConstructionGuideEntry
        {
            Localization = "construction-presenter-arbitrary-step",
            Arguments = [("name", MachinePart.Id)],
            Icon = Icon,
        };
    }
}
