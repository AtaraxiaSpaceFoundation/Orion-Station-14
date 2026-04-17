using Content.Shared.Popups;
using Content.Shared.White.Jukebox;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.White.Jukebox;

public sealed class JukeboxBUI : BoundUserInterface
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    private readonly SharedPopupSystem _sharedPopupSystem = default!;

    private JukeboxMenu? _window;
    public JukeboxBUI(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
        _sharedPopupSystem = _entityManager.System<SharedPopupSystem>();

        var uid = owner.Owner;

        if (!_entityManager.TryGetComponent<JukeboxComponent>(uid, out var jukeboxComponent))
        {
            _sharedPopupSystem.PopupEntity($"Тут нет JukeboxComponent, звоните кодерам", uid);
            return;
        }

        _window = new JukeboxMenu(jukeboxComponent);
        _window.RepeatButton.OnToggled += OnRepeatButtonToggled;
        _window.StopButton.OnPressed += OnStopButtonPressed;
        _window.EjectButton.OnPressed += OnEjectButtonPressed;
    }

    private void OnEjectButtonPressed(BaseButton.ButtonEventArgs obj)
    {
        SendMessage(new JukeboxEjectRequest());
    }

    private void OnStopButtonPressed(BaseButton.ButtonEventArgs obj)
    {
        SendMessage(new JukeboxStopRequest());
    }

    private void OnRepeatButtonToggled(BaseButton.ButtonToggledEventArgs obj)
    {
        SendMessage(new JukeboxRepeatToggled(obj.Pressed));
    }


    protected override void Open()
    {
        base.Open();

        if (_window == null)
        {
            Close();
            return;
        }

        _window.OpenCentered();
        _window.OnClose += Close;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;

        _window?.Dispose();
    }
}
