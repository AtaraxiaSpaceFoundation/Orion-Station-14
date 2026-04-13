using Content.Shared._Orion.ChameleonStamp;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Orion.ChameleonStamp;

[UsedImplicitly]
public sealed class ChameleonStampUserInterface : BoundUserInterface
{
    [ViewVariables]
    private ChameleonStampWindow? _window;

    public ChameleonStampUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ChameleonStampWindow>();

        _window.SetOwner(Owner);
        _window.OnClose += Close;
        _window.ApplyButton.OnButtonDown += ApplyButton_OnButtonDown;

        _window.OpenCentered();
    }

    private void ApplyButton_OnButtonDown(Robust.Client.UserInterface.Controls.BaseButton.ButtonEventArgs obj)
    {
        if (_window is null)
            return;

        if (!EntMan.TryGetComponent<ChameleonStampComponent>(Owner, out var chameleonStamp))
            return;

        var stampColorMeta = _window.StampColorOptionButton.GetItemMetadata(_window.StampColorOptionButton.SelectedId);
        var stampSpriteMeta = _window.StampSpriteOptionButton.GetItemMetadata(_window.StampSpriteOptionButton.SelectedId);
        var stampStateMeta = _window.StampStateOptionButton.GetItemMetadata(_window.StampStateOptionButton.SelectedId);

        chameleonStamp.SelectedStampColorPrototype = stampColorMeta is not null ? (string) stampColorMeta : chameleonStamp.DefaultStampPrototype;
        chameleonStamp.SelectedStampSpritePrototype = stampSpriteMeta is not null ? (string) stampSpriteMeta : chameleonStamp.DefaultStampPrototype;
        chameleonStamp.SelectedStampStatePrototype = stampStateMeta is not null ? (string) stampStateMeta : chameleonStamp.DefaultStampPrototype;

        chameleonStamp.CustomStampColor = _window.StampColorSelector.Color;

        chameleonStamp.CustomName = _window.StampMetaDataName.Text == "" ? null : _window.StampMetaDataName.Text;
        chameleonStamp.CustomDescription = _window.StampMetaDataDescription.Text == "" ? null : _window.StampMetaDataDescription.Text;
        chameleonStamp.StampedName = _window.StampedNameLineEdit.Text == "" ? null : _window.StampedNameLineEdit.Text;

        var selectedColor = chameleonStamp.CustomStampColor ?? _window.StampColorSelector.Color;

        SendMessage(new ChameleonStampApplySettingsMessage(
            chameleonStamp.SelectedStampColorPrototype,
            selectedColor,
            chameleonStamp.CustomName,
            chameleonStamp.CustomDescription,
            chameleonStamp.StampedName,
            chameleonStamp.SelectedStampSpritePrototype,
            chameleonStamp.SelectedStampStatePrototype));

        _window.UpdateVisuals();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _window != null)
        {
            _window.OnClose -= Close;
            _window.ApplyButton.OnButtonDown -= ApplyButton_OnButtonDown;
            _window.Dispose();
            _window = null;
        }

        base.Dispose(disposing);
    }
}
