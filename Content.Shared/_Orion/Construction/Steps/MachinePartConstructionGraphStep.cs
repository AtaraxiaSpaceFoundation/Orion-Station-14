using Content.Shared._Orion.Construction.Components;
using Content.Shared._Orion.Construction.Prototypes;
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
        examinedEvent.PushMarkup(string.IsNullOrEmpty(Name)
            ? Loc.GetString("construction-insert-entity-with-component",
                ("componentName", MachinePart.Id))
            : Loc.GetString("construction-insert-exact-entity",
                ("entityName", Loc.GetString(Name))));
    }
}
