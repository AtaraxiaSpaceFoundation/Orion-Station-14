using Robust.Shared.Serialization;

namespace Content.Shared._Orion.Economy;

[Serializable, NetSerializable]
public enum EconomyCardUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class EconomyCardWithdrawMessage(int amount, string? accountIdOverride) : BoundUserInterfaceMessage
{
    public int Amount = amount;
    public string? AccountIdOverride = accountIdOverride;
}

[Serializable, NetSerializable]
public sealed class EconomyCardBoundUiState(string? accountId, int balance) : BoundUserInterfaceState
{
    public string? AccountId = accountId;
    public int Balance = balance;
}
