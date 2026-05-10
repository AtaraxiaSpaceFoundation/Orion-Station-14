using Content.Server.Access.Systems;
using Content.Server._Orion.Economy.Components;
using Content.Server.Mind;
using Content.Server.Stack;
using Content.Shared.Access.Components;
using Content.Shared._Orion.Economy;
using Content.Shared.Mind.Components;
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

    public override void Initialize()
    {
        SubscribeLocalEvent<MindContainerComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<IdCardComponent, BoundUIOpenedEvent>(OnUiOpened);

        Subs.BuiEvents<IdCardComponent>(EconomyCardUiKey.Key,
            subs =>
        {
            subs.Event<EconomyCardWithdrawMessage>(OnWithdrawMessage);
        });
    }

    private void OnMindAdded(Entity<MindContainerComponent> ent, ref MindAddedMessage args)
    {
        if (!_idCard.TryFindIdCard(ent, out var idCard))
            return;

        var account = _bank.EnsurePlayerAccount(args.Mind.Owner, args.Mind.Comp);
        if (idCard.Comp.BankAccountId == account.AccountId)
            return;

        idCard.Comp.BankAccountId = account.AccountId;
        Dirty(idCard);
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

        if (!_bank.Withdraw(account, args.Amount, "Card withdrawal", GetNetEntity(user)))
            return;

        if (!_proto.TryIndex<StackPrototype>("Credit", out var stackProto))
            return;

        _stack.Spawn(args.Amount, stackProto, Transform(user).Coordinates);

        _ui.SetUiState(ent.Owner, EconomyCardUiKey.Key, new EconomyCardBoundUiState(ent.Comp.BankAccountId, account.Comp.Balance));
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
