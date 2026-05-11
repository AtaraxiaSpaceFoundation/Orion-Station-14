using Content.Server.Cargo.Systems;
using Content.Server.Popups;
using Content.Shared.Cargo.Components;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Server.Station.Systems;
using Content.Shared.Access.Systems;
using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server._Orion.Economy.Systems;

public sealed class PayrollSystem : EntitySystem
{
    [Dependency] private readonly BankSystem _bank = default!;
    [Dependency] private readonly CargoSystem _cargo = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;
    private readonly ISawmill _sawmill = Logger.GetSawmill("economy-payroll");

    private static readonly SoundSpecifier PayrollSound = new SoundPathSpecifier("/Audio/_Orion/Machines/twobeep_high.ogg");

    public void ProcessPayroll()
    {
        var query = EntityQueryEnumerator<MindComponent>();
        while (query.MoveNext(out var mindUid, out var mindComp))
        {
            if (mindComp.UserId == null || mindComp.OwnedEntity is not { } owned)
                continue;

            var stationUid = _station.GetOwningStation(owned);
            if (stationUid == null || !TryComp<StationBankAccountComponent>(stationUid, out var stationBank))
                continue;

            var account = _bank.EnsurePlayerAccount(mindUid, mindComp);
            var payrollData = GetPayrollData((mindUid, mindComp));
            if (payrollData == null)
                continue;

            var (job, salary, departmentAccount) = payrollData.Value;
            var departmentBalance = _cargo.GetBalanceFromAccount((stationUid.Value, stationBank), departmentAccount);
            if (departmentBalance <= 0)
                continue;

            var paid = Math.Min(salary, departmentBalance);

            if (!TryWithdrawDepartmentPayroll((stationUid.Value, stationBank), departmentAccount, departmentBalance, paid))
            {
                _sawmill.Warning($"Payroll withdrawal failed for station {stationUid.Value} department {departmentAccount} amount {paid}.");
                continue;
            }

            account.Department = departmentAccount;
            account.OwningStation = stationUid;
            account.JobId = job.ID;
            _bank.Deposit((mindUid, account), paid, "payroll", GetNetEntity(stationUid.Value), job.ID);
            NotifyPayroll(owned, account.AccountId, paid);
        }
    }

    private bool TryWithdrawDepartmentPayroll(Entity<StationBankAccountComponent?> stationBank, ProtoId<CargoAccountPrototype> departmentAccount, int departmentBalance, int amount)
    {
        if (amount <= 0)
            return false;

        if (departmentBalance < amount)
            return false;

        if (!TryComp<StationBankAccountComponent>(stationBank.Owner, out var resolvedBank))
            return false;

        _cargo.UpdateBankAccount((stationBank.Owner, resolvedBank), -amount, departmentAccount);
        return true;
    }

    private (JobPrototype Job, int Salary, ProtoId<CargoAccountPrototype> DepartmentAccount)? GetPayrollData(Entity<MindComponent> mind)
    {
        foreach (var role in mind.Comp.MindRoles)
        {
            if (!TryComp<MindRoleComponent>(role, out var mindRole) || mindRole.JobPrototype == null)
                continue;

            var job = _proto.Index(mindRole.JobPrototype.Value);
            if (job.Salary == null || job.PayrollDepartmentAccount == null || job.Salary <= 0)
                continue;

            return (job, job.Salary.Value, job.PayrollDepartmentAccount.Value);
        }

        return null;
    }

    private void NotifyPayroll(EntityUid recipient, string accountId, int amount)
    {
        if (!_idCard.TryFindIdCard(recipient, out var idCard) || idCard.Comp.BankAccountId != accountId)
            return;

        var popupText = Loc.GetString("payroll-popup-received", ("amount", amount));
        _popup.PopupEntity(popupText, recipient, recipient);
        _audio.PlayPvs(PayrollSound, Transform(recipient).Coordinates);
    }
}
