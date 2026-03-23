using Content.Shared._Orion.Research.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Orion.Research.UI;

[UsedImplicitly]
public sealed class DestructiveAnalyzerBoundUserInterface : BoundUserInterface
{
    private DestructiveAnalyzerMenu? _menu;

    public DestructiveAnalyzerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<DestructiveAnalyzerMenu>();
        _menu.OnClose += () => _menu = null;
        _menu.OnServerButtonPressed += () => SendMessage(new OpenResearchServerMenuMessage());
        _menu.OnAnalyzePressed += () => SendMessage(new DestructiveAnalyzerRunMessage());
        _menu.OnMethodSelected += method => SendMessage(new DestructiveAnalyzerSelectMethodMessage(method));
        _menu.OnEjectPressed += () => SendMessage(new DestructiveAnalyzerEjectMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is DestructiveAnalyzerBoundInterfaceState cast)
            _menu?.UpdateState(cast);
    }
}
