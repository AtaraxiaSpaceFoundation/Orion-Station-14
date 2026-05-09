using Content.Shared.Cargo.Prototypes;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared._Orion.Economy.Prototypes;

[Prototype("payrollSalary")]
public sealed class PayrollSalaryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public ProtoId<JobPrototype> Job;

    [DataField(required: true)]
    public int Salary;

    [DataField(required: true)]
    public ProtoId<CargoAccountPrototype> DepartmentAccount;
}
