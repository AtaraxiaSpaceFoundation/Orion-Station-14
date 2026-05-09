using System.Linq;
using Content.Server.Cargo.Systems;
using Content.Shared.Cargo.Components;
using Content.Shared._Orion.Economy.Prototypes;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Server.Station.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Orion.Economy.Systems;

public sealed class PayrollSystem : EntitySystem
{
    [Dependency] private readonly BankSystem _bank = default!;
    [Dependency] private readonly CargoSystem _cargo = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private TimeSpan _nextPayroll;
    private readonly TimeSpan _payrollDelay = TimeSpan.FromMinutes(6);

    public override void Update(float frameTime)
    {
        if (_timing.CurTime < _nextPayroll)
            return;

        _nextPayroll = _timing.CurTime + _payrollDelay;
        ProcessPayroll();
    }

    private void ProcessPayroll()
    {
        var salaries = _proto.EnumeratePrototypes<PayrollSalaryPrototype>().ToDictionary(p => p.Job, p => p);

        var query = EntityQueryEnumerator<MindComponent>();
        while (query.MoveNext(out var mindUid, out var mindComp))
        {
            if (mindComp.UserId == null || mindComp.OwnedEntity is not { } owned)
                continue;

            var stationUid = _station.GetOwningStation(owned);
            if (stationUid == null || !TryComp<StationBankAccountComponent>(stationUid, out var stationBank))
                continue;

            var account = _bank.EnsurePlayerAccount(mindUid, mindComp);
            var salary = GetSalaryData((mindUid, mindComp), salaries);
            if (salary == null)
                continue;

            var departmentBalance = _cargo.GetBalanceFromAccount((stationUid.Value, stationBank), salary.DepartmentAccount);
            if (departmentBalance <= 0)
                continue;

            var paid = Math.Min(salary.Salary, departmentBalance);
            _cargo.UpdateBankAccount((stationUid.Value, stationBank), -paid, salary.DepartmentAccount);

            account.Department = salary.DepartmentAccount;
            _bank.Deposit((mindUid, account), paid, $"Payroll: {salary.Job}", GetNetEntity(stationUid.Value));
        }
    }

    private PayrollSalaryPrototype? GetSalaryData(Entity<MindComponent> mind, Dictionary<ProtoId<JobPrototype>, PayrollSalaryPrototype> salaries)
    {
        foreach (var role in mind.Comp.MindRoles)
        {
            if (!TryComp<JobRoleComponent>(role, out _) || !TryComp<MindRoleComponent>(role, out var mindRole) || mindRole.JobPrototype == null)
                continue;

            if (salaries.TryGetValue(mindRole.JobPrototype.Value, out var data))
                return data;
        }

        return null;
    }
}
