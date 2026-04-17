using Content.Shared.Popups;
using Content.Shared.White.Jukebox;
using Robust.Client.GameObjects;

namespace Content.Client.White.Jukebox;

public sealed class TapeCreatorBUI : BoundUserInterface
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    private readonly SharedPopupSystem _sharedPopupSystem = default!;

    private TapeCreatorMenu? _window;

    public TapeCreatorBUI(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
        _sharedPopupSystem = _entityManager.System<SharedPopupSystem>();

        var uid = owner.Owner;

        if (!_entityManager.TryGetComponent<TapeCreatorComponent>(uid, out var tapeCreatorComponent))
        {
            _sharedPopupSystem.PopupEntity($"Тут нет TapeCreatorComponent, звоните кодерам", uid);
            return;
        }

        _window = new TapeCreatorMenu(tapeCreatorComponent);
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
