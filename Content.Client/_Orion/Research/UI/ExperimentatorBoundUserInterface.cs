using Content.Shared._Orion.Research.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Orion.Research.UI;

[UsedImplicitly]
public sealed class ExperimentatorBoundUserInterface : BoundUserInterface
{
    private ExperimentatorMenu? _menu;

    public ExperimentatorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<ExperimentatorMenu>();
        _menu.OnServerButtonPressed += () => SendMessage(new OpenResearchServerMenuMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is ExperimentatorBoundInterfaceState cast)
            _menu?.UpdateState(cast);
    }
}
