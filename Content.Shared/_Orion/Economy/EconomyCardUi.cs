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
    public readonly int Amount = amount;
    public readonly string? AccountIdOverride = accountIdOverride;
}

[Serializable, NetSerializable]
public sealed class EconomyCardSelectAccountMessage(string? accountIdOverride) : BoundUserInterfaceMessage
{
    public readonly string? AccountIdOverride = accountIdOverride;
}

[Serializable, NetSerializable]
public sealed class EconomyCardBoundUiState(string? accountId, int balance) : BoundUserInterfaceState
{
    public readonly string? AccountId = accountId;
    public readonly int Balance = balance;
}
