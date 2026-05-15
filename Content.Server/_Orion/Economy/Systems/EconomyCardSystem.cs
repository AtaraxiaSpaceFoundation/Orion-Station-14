using Content.Server.Access.Systems;
using Content.Server._Orion.Economy.Components;
using Content.Server.Mind;
using Content.Server.Stack;
using Content.Server.Station.Systems;
using Content.Shared.Access.Components;
using Content.Shared._Orion.Economy;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.Stacks;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server._Orion.Economy.Systems;

public sealed class EconomyCardSystem : EntitySystem
{
    [Dependency] private readonly BankSystem _bank = default!;
    [Dependency] private readonly IdCardSystem _idCard = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly StackSystem _stack = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly SharedJobSystem _jobs = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private static readonly ProtoId<StackPrototype> HolochipStackId = "CreditHolochip";
    private static readonly ProtoId<StackPrototype> CreditStackId = "Credit";

    private float _uiRefreshAccumulator;
    private readonly Dictionary<EntityUid, string> _openUiAccounts = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<MindContainerComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<RoleAddedEvent>(OnRoleAdded);
        SubscribeLocalEvent<IdCardComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<IdCardComponent, BoundUIClosedEvent>(OnUiClosed);
        SubscribeLocalEvent<IdCardComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<IdCardComponent, AfterInteractEvent>(OnAfterInteract);

        Subs.BuiEvents<IdCardComponent>(EconomyCardUiKey.Key,
            subs =>
        {
            subs.Event<EconomyCardWithdrawMessage>(OnWithdrawMessage);
            subs.Event<EconomyCardSelectAccountMessage>(OnSelectAccountMessage);
        });
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _uiRefreshAccumulator += frameTime;
        if (_uiRefreshAccumulator < 1f)
            return;

        _uiRefreshAccumulator = 0f;
        SyncAccountsOwningStations();

        var closedUis = new List<EntityUid>();
        foreach (var (uid, accountId) in _openUiAccounts)
        {
            if (!_ui.IsUiOpen(uid, EconomyCardUiKey.Key))
            {
                closedUis.Add(uid);
                continue;
            }

            if (string.IsNullOrWhiteSpace(accountId) || !_bank.TryFindAccountById(accountId, out var account))
            {
                _ui.SetUiState(uid, EconomyCardUiKey.Key, new EconomyCardBoundUiState(accountId, 0));
                continue;
            }

            _ui.SetUiState(uid, EconomyCardUiKey.Key, new EconomyCardBoundUiState(accountId, account.Comp.Balance));
        }

        foreach (var uid in closedUis)
        {
            _openUiAccounts.Remove(uid);
        }
    }

    private void SyncAccountsOwningStations()
    {
        var query = EntityQueryEnumerator<MindComponent>();
        while (query.MoveNext(out var mindUid, out var mind))
        {
            if (!IsHumanoidMind(mind))
                continue;

            if (mind.OwnedEntity is not { } owned)
                continue;

            if (_station.GetOwningStation(owned) is not { } stationUid)
                continue;

            var account = _bank.EnsurePlayerAccount(mindUid, mind);
            if (account.OwningStation == stationUid)
                continue;

            account.OwningStation = stationUid;
        }
    }

    private void OnMindAdded(Entity<MindContainerComponent> ent, ref MindAddedMessage args)
    {
        if (!IsHumanoidMind(args.Mind.Comp))
            return;

        var account = _bank.EnsurePlayerAccount(args.Mind.Owner, args.Mind.Comp);

        if (args.Mind.Comp.OwnedEntity is { } owned && _station.GetOwningStation(owned) is { } stationUid)
            account.OwningStation = stationUid;

        EnsureStartingPayroll(args.Mind.Owner, args.Mind.Comp, account);

        if (!_idCard.TryFindIdCard(ent, out var idCard))
            return;

        if (idCard.Comp.BankAccountId == account.AccountId)
            return;

        idCard.Comp.BankAccountId = account.AccountId;
        Dirty(idCard);
    }

    private void OnRoleAdded(RoleAddedEvent args)
    {
        var account = _bank.EnsurePlayerAccount(args.MindId, args.Mind);
        EnsureStartingPayroll(args.MindId, args.Mind, account);
    }

    private void EnsureStartingPayroll(EntityUid mindUid, MindComponent mind, StationAccountComponent account)
    {
        if (account.StartingPayrollReceived || !TryGetStartingPayrollData((mindUid, mind), out var payrollData))
            return;

        if (!_bank.Deposit((mindUid, account), payrollData.Salary, "starting-payroll", reasonData: payrollData.JobId))
            return;

        account.Department ??= payrollData.Department;
        account.JobId ??= payrollData.JobId;
        account.StartingPayrollReceived = true;
    }

    private bool TryGetStartingPayrollData(Entity<MindComponent> mind, out (ProtoId<CargoAccountPrototype>? Department, string JobId, int Salary) payrollData)
    {
        if (_jobs.MindTryGetJob(mind.Owner, out var job) && job.Salary is > 0)
        {
            payrollData = (job.PayrollDepartmentAccount, job.ID, job.Salary.Value);
            return true;
        }

        payrollData = default;
        return false;
    }

    private void OnUiOpened(Entity<IdCardComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (args.Actor is not { Valid: true } user)
            return;

        if (!ResolveAccount(ent, user, out var account))
        {
            _ui.SetUiState(ent.Owner, EconomyCardUiKey.Key, new EconomyCardBoundUiState(ent.Comp.BankAccountId, 0));
            return;
        }

        _openUiAccounts[ent.Owner] = account.Comp.AccountId;
        _ui.SetUiState(ent.Owner, EconomyCardUiKey.Key, new EconomyCardBoundUiState(account.Comp.AccountId, account.Comp.Balance));
    }

    private void OnUiClosed(Entity<IdCardComponent> ent, ref BoundUIClosedEvent args)
    {
        _openUiAccounts.Remove(ent.Owner);
    }

    private void OnWithdrawMessage(Entity<IdCardComponent> ent, ref EconomyCardWithdrawMessage args)
    {
        if (args.Actor is not { Valid: true } user || args.Amount <= 0)
            return;

        if (!ResolveAccount(ent, user, out var account, args.AccountIdOverride))
            return;

        if (account.Comp.BeingCrabbed)
        {
            _popup.PopupEntity(Loc.GetString("protocol-crab17-bank-card-warning"), ent, user, PopupType.MediumCaution);
            return;
        }

        if (!_proto.TryIndex(HolochipStackId, out var stackProto))
            return;

        if (!_bank.Withdraw(account, args.Amount, "card-withdrawal", GetNetEntity(user)))
            return;

        var holochip = _stack.Spawn(args.Amount, stackProto, Transform(user).Coordinates);
        _hands.PickupOrDrop(user, holochip);

        _openUiAccounts[ent.Owner] = account.Comp.AccountId;
        _ui.SetUiState(ent.Owner, EconomyCardUiKey.Key, new EconomyCardBoundUiState(account.Comp.AccountId, account.Comp.Balance));
    }

    private void OnSelectAccountMessage(Entity<IdCardComponent> ent, ref EconomyCardSelectAccountMessage args)
    {
        if (args.Actor is not { Valid: true } user)
            return;

        if (!ResolveAccount(ent, user, out var account, args.AccountIdOverride))
        {
            var accountId = string.IsNullOrWhiteSpace(args.AccountIdOverride)
                ? ent.Comp.BankAccountId
                : args.AccountIdOverride.Trim();

            _openUiAccounts[ent.Owner] = accountId ?? string.Empty;
            _ui.SetUiState(ent.Owner, EconomyCardUiKey.Key, new EconomyCardBoundUiState(accountId, 0));
            return;
        }

        _openUiAccounts[ent.Owner] = account.Comp.AccountId;
        _ui.SetUiState(ent.Owner, EconomyCardUiKey.Key, new EconomyCardBoundUiState(account.Comp.AccountId, account.Comp.Balance));
    }

    private void OnInteractUsing(Entity<IdCardComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp(args.Used, out StackComponent? usedStack) || usedStack.Count <= 0)
            return;

        if (usedStack.StackTypeId != HolochipStackId && usedStack.StackTypeId != CreditStackId)
            return;

        args.Handled = TryDepositStackToCard(ent, args.User, args.Used, usedStack);
    }

    private void OnAfterInteract(Entity<IdCardComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { Valid: true } target)
            return;

        if (!TryComp(target, out StackComponent? targetStack) || targetStack.Count <= 0)
            return;

        if (targetStack.StackTypeId != HolochipStackId && targetStack.StackTypeId != CreditStackId)
            return;

        args.Handled = TryDepositStackToCard(ent, args.User, target, targetStack);
    }

    private bool TryDepositStackToCard(Entity<IdCardComponent> card, EntityUid user, EntityUid stackUid, StackComponent stack)
    {
        if (!ResolveAccount(card, user, out var account))
            return false;

        var amount = stack.Count;
        if (!_bank.Deposit(account, amount, "card-deposit", GetNetEntity(user)))
            return false;

        _stack.SetCount(stackUid, 0, stack);
        _openUiAccounts[card.Owner] = account.Comp.AccountId;
        _ui.SetUiState(card.Owner, EconomyCardUiKey.Key, new EconomyCardBoundUiState(account.Comp.AccountId, account.Comp.Balance));
        return true;
    }

    private bool ResolveAccount(Entity<IdCardComponent> card, EntityUid user, out Entity<StationAccountComponent> account, string? accountOverride = null)
    {
        account = default;

        if (!_mind.TryGetMind(user, out _, out _))
            return false;

        var accountId = string.IsNullOrWhiteSpace(accountOverride)
            ? card.Comp.BankAccountId
            : accountOverride.Trim();

        if (string.IsNullOrWhiteSpace(accountId))
            return false;

        return _bank.TryFindAccountById(accountId, out account);
    }

    private bool IsHumanoidMind(MindComponent mind)
    {
        return mind.OwnedEntity is { } owned && HasComp<HumanoidAppearanceComponent>(owned);
    }
}
