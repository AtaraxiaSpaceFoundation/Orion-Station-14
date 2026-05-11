using Content.Server.Access.Systems;
using Content.Server._Orion.Economy.Components;
using Content.Server.Mind;
using Content.Server.Stack;
using Content.Server.Station.Systems;
using Content.Shared.Access.Components;
using Content.Shared._Orion.Economy;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
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

    private static readonly ProtoId<StackPrototype> CreditStackId = "Credit";

    private float _uiRefreshAccumulator;

    public override void Initialize()
    {
        SubscribeLocalEvent<MindContainerComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<IdCardComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<IdCardComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<IdCardComponent, AfterInteractEvent>(OnAfterInteract);

        Subs.BuiEvents<IdCardComponent>(EconomyCardUiKey.Key,
            subs =>
        {
            subs.Event<EconomyCardWithdrawMessage>(OnWithdrawMessage);
        });
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _uiRefreshAccumulator += frameTime;
        if (_uiRefreshAccumulator < 1f)
            return;

        _uiRefreshAccumulator = 0f;

        var query = EntityQueryEnumerator<IdCardComponent>();
        while (query.MoveNext(out var uid, out var card))
        {
            if (!_ui.IsUiOpen(uid, EconomyCardUiKey.Key))
                continue;

            if (string.IsNullOrWhiteSpace(card.BankAccountId) || !_bank.TryFindAccountById(card.BankAccountId, out var account))
            {
                _ui.SetUiState(uid, EconomyCardUiKey.Key, new EconomyCardBoundUiState(card.BankAccountId, 0));
                continue;
            }

            _ui.SetUiState(uid, EconomyCardUiKey.Key, new EconomyCardBoundUiState(card.BankAccountId, account.Comp.Balance));
        }
    }

    private void OnMindAdded(Entity<MindContainerComponent> ent, ref MindAddedMessage args)
    {
        var account = _bank.EnsurePlayerAccount(args.Mind.Owner, args.Mind.Comp);

        if (args.Mind.Comp.OwnedEntity is { } owned && _station.GetOwningStation(owned) is { } stationUid)
            account.OwningStation ??= stationUid;

        if ((account.Department == null || account.JobId == null) && TryGetStartingPayrollData(args.Mind.Comp, out var payrollData))
        {
            account.Department ??= payrollData.Department;
            account.JobId ??= payrollData.JobId;
        }

        if (!_idCard.TryFindIdCard(ent, out var idCard))
            return;

        if (idCard.Comp.BankAccountId == account.AccountId)
            return;

        idCard.Comp.BankAccountId = account.AccountId;
        Dirty(idCard);
    }

    private bool TryGetStartingPayrollData(MindComponent mind, out (ProtoId<CargoAccountPrototype> Department, string JobId) payrollData)
    {
        foreach (var roleUid in mind.MindRoles)
        {
            if (!TryComp<MindRoleComponent>(roleUid, out var role) || role.JobPrototype == null)
                continue;

            var job = _proto.Index(role.JobPrototype.Value);
            if (job.PayrollDepartmentAccount == null)
                continue;

            payrollData = (job.PayrollDepartmentAccount.Value, job.ID);
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

        _ui.SetUiState(ent.Owner, EconomyCardUiKey.Key, new EconomyCardBoundUiState(ent.Comp.BankAccountId, account.Comp.Balance));
    }

    private void OnWithdrawMessage(Entity<IdCardComponent> ent, ref EconomyCardWithdrawMessage args)
    {
        if (args.Actor is not { Valid: true } user || args.Amount <= 0)
            return;

        if (!ResolveAccount(ent, user, out var account, args.AccountIdOverride))
            return;

        if (!_bank.Withdraw(account, args.Amount, "card_withdrawal", GetNetEntity(user)))
            return;

        if (!_proto.TryIndex(CreditStackId, out var stackProto))
            return;

        var credits = _stack.Spawn(args.Amount, stackProto, Transform(user).Coordinates);
        _hands.PickupOrDrop(user, credits);

        _ui.SetUiState(ent.Owner, EconomyCardUiKey.Key, new EconomyCardBoundUiState(ent.Comp.BankAccountId, account.Comp.Balance));
    }

    private void OnInteractUsing(Entity<IdCardComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp(args.Used, out StackComponent? usedStack) || usedStack.StackTypeId != CreditStackId || usedStack.Count <= 0)
            return;

        args.Handled = TryDepositStackToCard(ent, args.User, args.Used, usedStack);
    }

    private void OnAfterInteract(Entity<IdCardComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { Valid: true } target)
            return;

        if (!TryComp(target, out StackComponent? targetStack) || targetStack.StackTypeId != CreditStackId || targetStack.Count <= 0)
            return;

        args.Handled = TryDepositStackToCard(ent, args.User, target, targetStack);
    }

    private bool TryDepositStackToCard(Entity<IdCardComponent> card, EntityUid user, EntityUid stackUid, StackComponent stack)
    {
        if (!ResolveAccount(card, user, out var account))
            return false;

        var amount = stack.Count;
        _bank.Deposit(account, amount, "card_deposit", GetNetEntity(user));
        _stack.SetCount(stackUid, 0, stack);
        _ui.SetUiState(card.Owner, EconomyCardUiKey.Key, new EconomyCardBoundUiState(card.Comp.BankAccountId, account.Comp.Balance));
        return true;
    }

    private bool ResolveAccount(Entity<IdCardComponent> card, EntityUid user, out Entity<StationAccountComponent> account, string? accountOverride = null)
    {
        account = default;

        if (!_mind.TryGetMind(user, out var mindUid, out var mindComp))
            return false;

        var ensured = _bank.EnsurePlayerAccount(mindUid, mindComp);

        var accountId = string.IsNullOrWhiteSpace(accountOverride) ? card.Comp.BankAccountId : accountOverride.Trim();

        if (!string.IsNullOrWhiteSpace(accountId) && string.Equals(accountId, ensured.AccountId, StringComparison.OrdinalIgnoreCase) && _bank.TryFindAccountById(accountId, out account))
            return true;

        if (string.IsNullOrWhiteSpace(card.Comp.BankAccountId))
            return false;

        account = (mindUid, ensured);
        return true;
    }
}
