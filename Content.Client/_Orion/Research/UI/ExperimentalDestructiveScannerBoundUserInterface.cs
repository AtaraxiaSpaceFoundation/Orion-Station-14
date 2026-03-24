using Content.Shared._Orion.Research.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Orion.Research.UI;

[UsedImplicitly]
public sealed class ExperimentalDestructiveScannerBoundUserInterface : BoundUserInterface
{
    private ExperimentalDestructiveScannerMenu? _menu;

    public ExperimentalDestructiveScannerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<ExperimentalDestructiveScannerMenu>();
        _menu.OnServerButtonPressed += () => SendMessage(new OpenResearchServerMenuMessage());
        _menu.OnPerformPressed += () => SendMessage(new ExperimentalDestructiveScannerPerformMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is ExperimentalDestructiveScannerBoundInterfaceState cast)
            _menu?.UpdateState(cast);
    }
}
