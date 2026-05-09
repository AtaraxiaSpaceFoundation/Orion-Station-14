using Content.Shared._Orion.Economy;
using Robust.Client.UserInterface;

namespace Content.Client._Orion.Economy.UI;

public sealed class EconomyCardBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private EconomyCardWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<EconomyCardWindow>();
        _window.Withdraw.OnPressed += _ =>
        {
            var amount = _window.Amount.Value;
            var account = string.IsNullOrWhiteSpace(_window.AccountId.Text) ? null : _window.AccountId.Text.Trim();
            SendMessage(new EconomyCardWithdrawMessage(amount, account));
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (_window == null || state is not EconomyCardBoundUiState cast)
            return;

        if (string.IsNullOrWhiteSpace(_window.AccountId.Text))
            _window.AccountId.Text = cast.AccountId ?? string.Empty;

        _window.BalanceLabel.Text = Loc.GetString("economy-card-balance-label", ("balance", cast.Balance));
    }
}
