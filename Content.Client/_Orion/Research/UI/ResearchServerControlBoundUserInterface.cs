using Content.Shared._Orion.Research.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Orion.Research.UI;

[UsedImplicitly]
public sealed class ResearchServerControlBoundUserInterface : BoundUserInterface
{
    private ResearchServerControlMenu? _menu;

    public ResearchServerControlBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<ResearchServerControlMenu>();
        _menu.OnToggleRequested += id => SendMessage(new ToggleServerGenerationMessage(id));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is ResearchServerControlBoundInterfaceState cast)
            _menu?.Populate(cast.Servers);
    }
}
