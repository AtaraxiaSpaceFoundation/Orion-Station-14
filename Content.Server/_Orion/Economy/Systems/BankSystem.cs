using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server._Orion.Economy.Components;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Database;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Random;

namespace Content.Server._Orion.Economy.Systems;

public sealed class BankSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private readonly ISawmill _sawmill = Logger.GetSawmill("economy-bank");

    public override void Initialize()
    {
        SubscribeLocalEvent<StationAccountComponent, ComponentStartup>(OnAccountStartup);
    }

    private void OnAccountStartup(Entity<StationAccountComponent> ent, ref ComponentStartup args)
    {
        if (TryComp<MindComponent>(ent, out var mind) && !string.IsNullOrWhiteSpace(mind.CharacterName))
            ent.Comp.OwnerName = mind.CharacterName;
    }

    public StationAccountComponent EnsurePlayerAccount(EntityUid mindUid, MindComponent? mind = null)
    {
        var account = EnsureComp<StationAccountComponent>(mindUid);

        if (!IsValidAccountId(account.AccountId))
            account.AccountId = GenerateUniqueAccountId();

        if (Resolve(mindUid, ref mind, false) && !string.IsNullOrWhiteSpace(mind.CharacterName) && account.OwnerName != mind.CharacterName)
            account.OwnerName = mind.CharacterName;

        return account;
    }

    private static bool IsValidAccountId(string? accountId)
    {
        if (string.IsNullOrWhiteSpace(accountId) || accountId.Length != 12)
            return false;

        return accountId.All(char.IsDigit);
    }

    private string GenerateUniqueAccountId()
    {
        Span<char> digits = stackalloc char[12];

        while (true)
        {
            digits[0] = (char) ('1' + _random.Next(9));
            for (var i = 1; i < digits.Length; i++)
            {
                digits[i] = (char) ('0' + _random.Next(10));
            }

            var candidate = new string(digits);
            if (!TryFindAccountById(candidate, out _))
                return candidate;
        }
    }

    public bool TryGetPlayerAccount(EntityUid playerEntity, out EntityUid mindUid, out StationAccountComponent account)
    {
        account = default!;
        mindUid = default;
        if (!_mind.TryGetMind(playerEntity, out mindUid, out _))
            return false;

        if (!TryComp(mindUid, out StationAccountComponent? found))
            return false;

        account = found;
        return true;
    }

    public bool TryFindAccountById(string accountId, out Entity<StationAccountComponent> account)
    {
        var query = EntityQueryEnumerator<StationAccountComponent>();
        while (query.MoveNext(out var uid, out var acc))
        {
            if (!string.Equals(acc.AccountId, accountId, StringComparison.OrdinalIgnoreCase))
                continue;

            account = (uid, acc);
            return true;
        }

        account = default;
        return false;
    }

    public bool TryGetDepartment(Entity<StationAccountComponent> account, out ProtoId<CargoAccountPrototype> department)
    {
        if (account.Comp.Department is { } accountDepartment)
        {
            department = accountDepartment;
            return true;
        }

        if (TryGetJobDepartment(account.Comp.JobId, out department))
        {
            account.Comp.Department = department;
            Dirty(account);
            return true;
        }

        if (!TryComp<MindComponent>(account.Owner, out var mind))
            return false;

        foreach (var role in mind.MindRoles)
        {
            if (!TryComp<MindRoleComponent>(role, out var mindRole) || mindRole.JobPrototype == null)
                continue;

            if (!_proto.TryIndex(mindRole.JobPrototype.Value, out var job) || job.PayrollDepartmentAccount is not { } payrollDepartment)
                continue;

            department = payrollDepartment;
            account.Comp.Department = department;
            Dirty(account);

            if (account.Comp.JobId != null)
                return true;

            account.Comp.JobId = job.ID;
            Dirty(account);

            return true;
        }

        return false;
    }

    private bool TryGetJobDepartment(string? jobId, out ProtoId<CargoAccountPrototype> department)
    {
        department = default;
        if (string.IsNullOrWhiteSpace(jobId) || !_proto.TryIndex<JobPrototype>(jobId, out var job) || job.PayrollDepartmentAccount is not { } payrollDepartment)
            return false;

        department = payrollDepartment;
        return true;
    }

    public static int GetBalance(Entity<StationAccountComponent> account)
    {
        return account.Comp.Balance;
    }

    public bool Deposit(Entity<StationAccountComponent> account, int amount, string reason, NetEntity? counterparty = null, string? reasonData = null)
    {
        if (amount <= 0)
            return false;

        return AdjustBalance(account, amount, reason, counterparty, reasonData);
    }

    public bool Withdraw(Entity<StationAccountComponent> account, int amount, string reason, NetEntity? counterparty = null, string? reasonData = null)
    {
        if (amount <= 0 || account.Comp.Balance < amount)
            return false;

        return AdjustBalance(account, -amount, reason, counterparty, reasonData);
    }

    public bool Transfer(Entity<StationAccountComponent> from, Entity<StationAccountComponent> to, int amount, string reason)
    {
        if (!Withdraw(from, amount, reason, GetNetEntity(to.Owner)))
            return false;

        if (Deposit(to, amount, reason, GetNetEntity(from.Owner)))
            return true;

        _sawmill.Error($"Transfer deposit failed. Attempting rollback from {to.Comp.AccountId} to {from.Comp.AccountId}. Amount: {amount}. Reason: {reason}");

        if (!Deposit(from, amount, $"rollback: {reason}", GetNetEntity(to.Owner)))
            _sawmill.Error($"Transfer rollback failed for account {from.Comp.AccountId}. Manual intervention may be required.");

        return false;
    }

    private bool AdjustBalance(Entity<StationAccountComponent> account, int delta, string reason, NetEntity? counterparty = null, string? reasonData = null)
    {
        if (delta == 0)
            return true;

        try
        {
            checked
            {
                account.Comp.Balance += delta;
            }

            AddTransaction(account, delta, reason, reasonData, counterparty);

            _adminLog.Add(LogType.Action, LogImpact.Low, $"Account {account.Comp.AccountId} ({account.Comp.OwnerName}) adjusted by {delta}. Reason: {reason}. New balance: {account.Comp.Balance}");
            return true;
        }
        catch (OverflowException)
        {
            _sawmill.Error($"Failed to adjust account {account.Comp.AccountId} by {delta}: integer overflow.");
            return false;
        }
    }

    private void AddTransaction(Entity<StationAccountComponent> account, int delta, string reason, string? reasonData, NetEntity? counterparty)
    {
        account.Comp.History.Add(new AccountTransaction(_timing.CurTime, delta, account.Comp.Balance, reason, reasonData, counterparty));
        if (account.Comp.History.Count <= account.Comp.MaxHistory)
            return;

        account.Comp.History.RemoveRange(0, account.Comp.History.Count - account.Comp.MaxHistory);
    }
}
