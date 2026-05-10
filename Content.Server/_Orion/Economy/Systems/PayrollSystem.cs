using Content.Server.Cargo.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Shared.Cargo.Components;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Server.Station.Systems;
using Content.Shared.Access.Systems;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Chat;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
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
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;
    private readonly ISawmill _sawmill = Logger.GetSawmill("economy-payroll");

    private TimeSpan _nextPayroll;
    private readonly TimeSpan _payrollDelay = TimeSpan.FromMinutes(6);

    private static readonly SoundSpecifier PayrollSound = new SoundPathSpecifier("/Audio/Effects/chime.ogg");

    public override void Initialize()
    {
        _nextPayroll = _timing.CurTime + _payrollDelay;
    }

    public void UpdatePayroll()
    {
        if (_timing.CurTime < _nextPayroll)
            return;

        _nextPayroll = _timing.CurTime + _payrollDelay;
        ProcessPayroll();
    }

    private void ProcessPayroll()
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

            if (!TryWithdrawDepartmentPayroll((stationUid.Value, stationBank), departmentAccount, paid))
            {
                _sawmill.Warning($"Payroll withdrawal failed for station {stationUid.Value} department {departmentAccount} amount {paid}.");
                continue;
            }

            account.Department = departmentAccount;
            _bank.Deposit((mindUid, account), paid, $"Payroll: {job.ID}", GetNetEntity(stationUid.Value));
            NotifyPayroll(owned, account.AccountId, paid);
        }
    }

    private bool TryWithdrawDepartmentPayroll(Entity<StationBankAccountComponent?> stationBank, ProtoId<CargoAccountPrototype> departmentAccount, int amount)
    {
        if (amount <= 0)
            return false;

        var current = _cargo.GetBalanceFromAccount(stationBank, departmentAccount);
        if (current < amount)
            return false;

        _cargo.UpdateBankAccount(stationBank, -amount, departmentAccount);
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
        _popup.PopupEntity(popupText, recipient, recipient, PopupType.Medium);
        _chat.TrySendInGameICMessage(recipient, popupText, InGameICChatType.Speak, ChatTransmitRange.HideChat, hideLog: true);
        _audio.PlayPvs(PayrollSound, Transform(recipient).Coordinates);
    }
}
