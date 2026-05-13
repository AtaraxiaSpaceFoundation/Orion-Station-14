using Content.Shared._Orion.Economy;
using Robust.Client.UserInterface;

namespace Content.Client._Orion.Economy.UI;

public sealed class EconomyCardBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private EconomyCardWindow? _window;
    private string? _lastAccountId;
    private int _lastBalance;
    private bool _manualExpanded;
    private bool _accountOverrideEdited;
    private bool _suppressAccountEditTracking;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<EconomyCardWindow>();

        _window.WithdrawButton.OnPressed += _ => TryWithdraw();
        _window.Quick10Button.OnPressed += _ => SetAmount(10);
        _window.Quick50Button.OnPressed += _ => SetAmount(50);
        _window.Quick100Button.OnPressed += _ => SetAmount(100);
        _window.QuickMaxPrimaryButton.OnPressed += _ => SetMaxAmount();
        _window.ManualAccountToggle.OnPressed += _ => ToggleManualAccount();
        _window.AmountInput.OnTextChanged += _ => UpdateWithdrawAvailability();
        _window.AccountId.OnTextChanged += _ => OnAccountInputChanged();

        UpdateDisplayedAccount();
        UpdateWithdrawAvailability();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (_window == null || state is not EconomyCardBoundUiState cast)
            return;

        _lastAccountId = cast.AccountId;
        _lastBalance = Math.Max(0, cast.Balance);

        _window.BalanceValueLabel.Text = _window.FormatCredits(_lastBalance);
        UpdateDisplayedAccount();

        if (!_manualExpanded && !_accountOverrideEdited && string.IsNullOrWhiteSpace(_window.AccountId.Text))
        {
            _suppressAccountEditTracking = true;
            _window.AccountId.Text = _lastAccountId ?? string.Empty;
            _suppressAccountEditTracking = false;
        }

        UpdateWithdrawAvailability();
    }

    private void OnAccountInputChanged()
    {
        if (_window == null)
            return;

        if (!_suppressAccountEditTracking)
        {
            var input = string.IsNullOrWhiteSpace(_window.AccountId.Text) ? null : _window.AccountId.Text.Trim();
            _accountOverrideEdited = !string.Equals(input, _lastAccountId, StringComparison.Ordinal);
        }

        SendMessage(new EconomyCardSelectAccountMessage(GetEffectiveAccountId()));

        UpdateDisplayedAccount();
        UpdateWithdrawAvailability();
    }

    private void UpdateDisplayedAccount()
    {
        if (_window == null)
            return;

        _window.MaskedAccountLabel.Text = _window.MaskAccountId(GetEffectiveAccountId());
    }

    private string? GetEffectiveAccountId()
    {
        if (_window == null)
            return _lastAccountId;

        var manualInput = string.IsNullOrWhiteSpace(_window.AccountId.Text) ? null : _window.AccountId.Text.Trim();
        if (_accountOverrideEdited)
            return manualInput;

        return manualInput ?? _lastAccountId;
    }

    private void TryWithdraw()
    {
        if (_window == null)
            return;

        var effectiveAccount = GetEffectiveAccountId();
        if (string.IsNullOrWhiteSpace(effectiveAccount))
            return;

        var amount = GetSafeAmount();
        if (amount <= 0)
            return;

        SendMessage(new EconomyCardWithdrawMessage(amount, effectiveAccount));
    }

    private int GetSafeAmount()
    {
        if (_window == null)
            return 0;

        var text = _window.AmountInput.Text.Trim();
        if (!int.TryParse(text, out var amount) || amount <= 0)
            return 0;

        return _lastBalance > 0 ? Math.Clamp(amount, 1, _lastBalance) : amount;
    }

    private void SetAmount(int amount)
    {
        if (_window == null)
            return;

        var safe = Math.Max(1, amount);
        if (_lastBalance > 0)
            safe = Math.Clamp(safe, 1, _lastBalance);

        _window.AmountInput.Text = safe.ToString();
        UpdateWithdrawAvailability();
    }

    private void SetMaxAmount()
    {
        if (_window == null)
            return;

        var max = Math.Max(1, _lastBalance);
        _window.AmountInput.Text = max.ToString();
        UpdateWithdrawAvailability();
    }

    private void ToggleManualAccount()
    {
        if (_window == null)
            return;

        _manualExpanded = !_manualExpanded;
        _window.ManualAccountContainer.Visible = _manualExpanded;

        if (!_manualExpanded)
            _accountOverrideEdited = false;
    }

    private void UpdateWithdrawAvailability()
    {
        if (_window == null)
            return;

        var hasAccount = !string.IsNullOrWhiteSpace(GetEffectiveAccountId());
        var hasFunds = _lastBalance > 0;
        var amount = GetSafeAmount();
        var canWithdraw = hasAccount && hasFunds && amount > 0;

        _window.WithdrawButton.Disabled = !canWithdraw;
        _window.QuickMaxPrimaryButton.Disabled = !hasFunds;

        if (!hasAccount)
        {
            _window.WithdrawStatusLabel.Text = Loc.GetString("economy-card-status-no-account");
            _window.WithdrawStatusLabel.FontColorOverride = Color.FromHex("#E06C75");
            _window.WithdrawButton.ModulateSelfOverride = Color.FromHex("#C85D66");
            return;
        }

        if (!hasFunds)
        {
            _window.WithdrawStatusLabel.Text = Loc.GetString("economy-card-status-no-funds");
            _window.WithdrawStatusLabel.FontColorOverride = Color.FromHex("#E06C75");
            _window.WithdrawButton.ModulateSelfOverride = Color.FromHex("#C85D66");
            return;
        }

        _window.WithdrawStatusLabel.Text = Loc.GetString("economy-card-withdraw-hint");
        _window.WithdrawStatusLabel.FontColorOverride = Color.FromHex("#9AA4B2");
        _window.WithdrawButton.ModulateSelfOverride = Color.FromHex("#53C78F");
    }
}
