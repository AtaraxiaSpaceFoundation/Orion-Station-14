using System.Linq;
using Content.Server._Orion.Economy.Components;
using Content.Server.Cargo.Components;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Emag.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Roles;
using Content.Shared.UserInterface;

namespace Content.Server.Cargo.Systems;

public sealed partial class CargoSystem
{
    private bool _allowPrimaryAccountAllocation;
    private bool _allowPrimaryCutAdjustment;

    private void InitializeFunds() // Orion-Edit: Was public
    {
        SubscribeLocalEvent<CargoOrderConsoleComponent, CargoConsoleWithdrawFundsMessage>(OnWithdrawFunds);
        SubscribeLocalEvent<CargoOrderConsoleComponent, CargoConsoleToggleLimitMessage>(OnToggleLimit);
        SubscribeLocalEvent<FundingAllocationConsoleComponent, SetFundingAllocationBuiMessage>(OnSetFundingAllocation);
        SubscribeLocalEvent<FundingAllocationConsoleComponent, BeforeActivatableUIOpenEvent>(OnFundAllocationBuiOpen);
        SubscribeLocalEvent<StationBankAccountComponent, ComponentStartup>(OnStationBankStartup); // Orion

        _cfg.OnValueChanged(CCVars.AllowPrimaryAccountAllocation, enabled => { _allowPrimaryAccountAllocation = enabled; }, true);
        _cfg.OnValueChanged(CCVars.AllowPrimaryCutAdjustment, enabled => { _allowPrimaryCutAdjustment = enabled; }, true);
    }

    // Orion-Start
    private void OnStationBankStartup(Entity<StationBankAccountComponent> ent, ref ComponentStartup args)
    {
        var changed = false;

        foreach (var (account, _) in ent.Comp.Accounts)
        {
            if (!_protoMan.TryIndex(account, out var proto) || proto.BudgetFundingAmount <= 0)
                continue;

            if (ent.Comp.NextBudgetFundingTime.ContainsKey(account))
                continue;

            ent.Comp.NextBudgetFundingTime[account] = Timing.CurTime + proto.BudgetFundingDelay;
            changed = true;
        }

        if (changed)
            Dirty(ent);
    }
    // Orion-End

    private void OnWithdrawFunds(Entity<CargoOrderConsoleComponent> ent, ref CargoConsoleWithdrawFundsMessage args)
    {
        if (_station.GetOwningStation(ent) is not { } station ||
            !TryComp<StationBankAccountComponent>(station, out var bank))
            return;

        if (args.Account == ent.Comp.Account ||
            args.Amount <= 0 ||
            args.Amount > GetBalanceFromAccount((station, bank), ent.Comp.Account) * ent.Comp.TransferLimit)
            return;

        if (Timing.CurTime < ent.Comp.NextAccountActionTime)
            return;

        if (!_accessReaderSystem.IsAllowed(args.Actor, ent))
        {
            ConsolePopup(args.Actor, Loc.GetString("cargo-console-order-not-allowed"));
            PlayDenySound(ent, ent.Comp);
            return;
        }

        ent.Comp.NextAccountActionTime = Timing.CurTime + ent.Comp.AccountActionDelay;
        UpdateBankAccount((station, bank), -args.Amount,  ent.Comp.Account, dirty: false);
        _audio.PlayPvs(ApproveSound, ent);

        var tryGetIdentityShortInfoEvent = new TryGetIdentityShortInfoEvent(ent, args.Actor);
        RaiseLocalEvent(tryGetIdentityShortInfoEvent);

        var ourAccount = _protoMan.Index(ent.Comp.Account);
        if (args.Account == null)
        {
            var stackPrototype = _protoMan.Index(ent.Comp.CashType);
            _stack.Spawn(args.Amount, stackPrototype, Transform(ent).Coordinates);

            if (!_emag.CheckFlag(ent, EmagType.Interaction))
            {
                var msg = Loc.GetString("cargo-console-fund-withdraw-broadcast",
                    ("name", tryGetIdentityShortInfoEvent.Title ?? Loc.GetString("cargo-console-fund-transfer-user-unknown")),
                    ("amount", args.Amount),
                    ("name1", Loc.GetString(ourAccount.Name)),
                    ("code1", Loc.GetString(ourAccount.Code)));
                _radio.SendRadioMessage(ent, msg, ourAccount.RadioChannel, ent, escapeMarkup: false);
            }
        }
        else
        {
            var otherAccount = _protoMan.Index(args.Account.Value);
            UpdateBankAccount((station, bank), args.Amount, args.Account.Value);

            if (!_emag.CheckFlag(ent, EmagType.Interaction))
            {
                var msg = Loc.GetString("cargo-console-fund-transfer-broadcast",
                    ("name", tryGetIdentityShortInfoEvent.Title ?? Loc.GetString("cargo-console-fund-transfer-user-unknown")),
                    ("amount", args.Amount),
                    ("name1", Loc.GetString(ourAccount.Name)),
                    ("code1", Loc.GetString(ourAccount.Code)),
                    ("name2", Loc.GetString(otherAccount.Name)),
                    ("code2", Loc.GetString(otherAccount.Code)));
                _radio.SendRadioMessage(ent, msg, ourAccount.RadioChannel, ent, escapeMarkup: false);
                _radio.SendRadioMessage(ent, msg, otherAccount.RadioChannel, ent, escapeMarkup: false);
            }
        }
    }

    private void OnToggleLimit(Entity<CargoOrderConsoleComponent> ent, ref CargoConsoleToggleLimitMessage args)
    {
        if (!_accessReaderSystem.FindAccessTags(args.Actor).Intersect(ent.Comp.RemoveLimitAccess).Any())
        {
            ConsolePopup(args.Actor, Loc.GetString("cargo-console-order-not-allowed"));
            PlayDenySound(ent, ent.Comp);
            return;
        }

        _audio.PlayPvs(ent.Comp.ToggleLimitSound, ent);
        ent.Comp.TransferUnbounded = !ent.Comp.TransferUnbounded;
        Dirty(ent);
    }


    private void OnSetFundingAllocation(Entity<FundingAllocationConsoleComponent> ent, ref SetFundingAllocationBuiMessage args)
    {
        if (_station.GetOwningStation(ent) is not { } station ||
            !TryComp<StationBankAccountComponent>(station, out var bank))
            return;

        // Orion-Edit-Start
        var expectedEditableKeys = bank.RevenueDistribution.Keys
            .Where(account => _allowPrimaryAccountAllocation || account != bank.PrimaryAccount)
            .ToHashSet();
        var expectedCount = expectedEditableKeys.Count;

        if (args.Percents.Count != expectedCount || !args.Percents.Keys.ToHashSet().SetEquals(expectedEditableKeys))
            return;
        // Orion-Edit-End

        var differs = false;
        foreach (var (account, percent) in args.Percents)
        {
            // Orion-Edit-Start
            if (bank.RevenueDistribution.TryGetValue(account, out var currentPercent) && percent == (int) Math.Round(currentPercent * 100))
                continue;

            differs = true;
            break;
            // Orion-Edit-End
        }
        differs = differs || args.PrimaryCut != bank.PrimaryCut || args.LockboxCut != bank.LockboxCut;

        if (!differs)
            return;

        if (args.Percents.Values.Sum() != 100)
            return;

//        var primaryCut = bank.RevenueDistribution[bank.PrimaryAccount]; // Orion-Edit

        // Orion-Edit-Start
        var updatedDistribution = args.Percents.ToDictionary(kv => kv.Key, kv => kv.Value / 100.0);

        if (!_allowPrimaryAccountAllocation)
            updatedDistribution[bank.PrimaryAccount] = 0;

        bank.RevenueDistribution = updatedDistribution;
        // Orion-Edit-End

        if (_allowPrimaryCutAdjustment && args.PrimaryCut is >= 0.0 and <= 1.0)
        {
            bank.PrimaryCut = args.PrimaryCut;
        }
        if (_lockboxCutEnabled && args.LockboxCut is >= 0.0 and <= 1.0)
        {
            bank.LockboxCut = args.LockboxCut;
        }

        Dirty(station, bank);

        _audio.PlayPvs(ent.Comp.SetDistributionSound, ent);
        _adminLogger.Add(
            LogType.Action,
            LogImpact.Medium,
            $"{ToPrettyString(args.Actor):player} set station {ToPrettyString(station)} fund distribution: {string.Join(',', bank.RevenueDistribution.Select(p => $"{p.Key}: {p.Value}").ToList())}, primary cut: {bank.PrimaryCut}, lockbox cut: {bank.LockboxCut}");
    }

    private void OnFundAllocationBuiOpen(Entity<FundingAllocationConsoleComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
/* // Orion-Edit
        if (_station.GetOwningStation(ent) is { } station)
            _uiSystem.SetUiState(ent.Owner, FundingAllocationConsoleUiKey.Key, new FundingAllocationConsoleBuiState(GetNetEntity(station)));
*/

        // Orion-Start
        if (_station.GetOwningStation(ent) is not { } station)
            return;

        _uiSystem.SetUiState(ent.Owner, FundingAllocationConsoleUiKey.Key, BuildFundingState(station));
        // Orion-End
    }

    // Orion-Start
    private FundingAllocationConsoleBuiState BuildFundingState(EntityUid station)
    {
        var accounts = new List<FundingAllocationEconomyAccountData>();
        var transactions = new List<FundingAllocationTransactionData>();
        var transactionIndex = 0;

        var accountQuery = EntityQueryEnumerator<StationAccountComponent>();
        while (accountQuery.MoveNext(out var accountUid, out var account))
        {
            if (account.OwningStation != station)
                continue;

            if (!ShouldShowEconomyAccount(account))
                continue;

            var departmentId = account.Department is { } department ? department.Id : null;
            accounts.Add(new FundingAllocationEconomyAccountData(GetNetEntity(accountUid), account.AccountId, account.OwnerName, account.Balance, departmentId, account.JobId));

            foreach (var transaction in account.History)
            {
                transactions.Add(new FundingAllocationTransactionData(
                    transactionIndex++,
                    transaction.Time,
                    transaction.Delta,
                    transaction.Reason,
                    transaction.ReasonData,
                    GetNetEntity(accountUid),
                    transaction.Receiver));
            }
        }

        transactions.Sort((a, b) => a.Time.CompareTo(b.Time));

        return new FundingAllocationConsoleBuiState(GetNetEntity(station), accounts, transactions);
    }

    private void UpdateEconomyInterfaces(float frameTime)
    {
        _uiRefreshAccumulator += frameTime;
        if (_uiRefreshAccumulator < 1f)
            return;

        _uiRefreshAccumulator = 0f;

        var query = EntityQueryEnumerator<FundingAllocationConsoleComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (_station.GetOwningStation(uid) is not { } station)
                continue;

            _uiSystem.SetUiState(uid, FundingAllocationConsoleUiKey.Key, BuildFundingState(station));
        }

        var palletQuery = EntityQueryEnumerator<CargoPalletConsoleComponent>();
        while (palletQuery.MoveNext(out var palletUid, out _))
        {
            UpdatePalletConsoleInterface(palletUid);
        }
    }

    private bool ShouldShowEconomyAccount(StationAccountComponent account)
    {
        if (string.IsNullOrWhiteSpace(account.JobId))
            return true;

        if (!_protoMan.TryIndex<JobPrototype>(account.JobId, out var job))
            return true;

        return job.PayrollFromStationBudget;
    }

    // Orion-End
}
