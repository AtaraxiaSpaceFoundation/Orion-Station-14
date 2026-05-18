using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Cargo.Components;

/// <summary>
/// A console that manipulates the distribution of revenue on the station.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedCargoSystem))]
public sealed partial class FundingAllocationConsoleComponent : Component
{
    /// <summary>
    /// Sound played when the budget distribution is set.
    /// </summary>
    [DataField]
    public SoundSpecifier SetDistributionSound = new SoundCollectionSpecifier("CargoPing");
}

[Serializable, NetSerializable]
public sealed class SetFundingAllocationBuiMessage : BoundUserInterfaceMessage
{
    public Dictionary<ProtoId<CargoAccountPrototype>, int> Percents;
    public double PrimaryCut;
    public double LockboxCut;

    public SetFundingAllocationBuiMessage(Dictionary<ProtoId<CargoAccountPrototype>, int> percents, double primaryCut, double lockboxCut)
    {
        Percents = percents;
        PrimaryCut = primaryCut;
        LockboxCut = lockboxCut;
    }
}

[Serializable, NetSerializable]
public sealed class FundingAllocationConsoleBuiState : BoundUserInterfaceState
{
    public NetEntity Station;
    // Orion-Start
    public List<FundingAllocationEconomyAccountData> EconomyAccounts;
    public List<FundingAllocationTransactionData> Transactions;
    // Orion-End

    public FundingAllocationConsoleBuiState(NetEntity station, List<FundingAllocationEconomyAccountData> economyAccounts, List<FundingAllocationTransactionData> transactions) // Orion-Edit
    {
        Station = station;
        EconomyAccounts = economyAccounts; // Orion
        Transactions = transactions; // Orion
    }
}

// Orion-Start
[Serializable, NetSerializable]
public sealed class FundingAllocationEconomyAccountData
{
    public NetEntity Account;
    public string AccountId;
    public string AccountName;
    public int Balance;
    public string? DepartmentId;
    public string? JobId;

    public FundingAllocationEconomyAccountData(NetEntity account, string accountId, string accountName, int balance, string? departmentId, string? jobId)
    {
        Account = account;
        AccountId = accountId;
        AccountName = accountName;
        Balance = balance;
        DepartmentId = departmentId;
        JobId = jobId;
    }
}

[Serializable, NetSerializable]
public sealed class FundingAllocationTransactionData
{
    public int Index;
    public TimeSpan Time;
    public int Delta;
    public string Reason;
    public string? ReasonData;
    public NetEntity Account;
    public NetEntity? Counterparty;

    public FundingAllocationTransactionData(int index, TimeSpan time, int delta, string reason, string? reasonData, NetEntity account, NetEntity? counterparty)
    {
        Index = index;
        Time = time;
        Delta = delta;
        Reason = reason;
        ReasonData = reasonData;
        Account = account;
        Counterparty = counterparty;
    }
}
// Orion-End

[Serializable, NetSerializable]
public enum FundingAllocationConsoleUiKey : byte
{
    Key,
}
