using Content.Shared._Orion.Bitrunning;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Orion.Bitrunning.UI.Netpod;

[UsedImplicitly]
public sealed class NetpodBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private NetpodWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<NetpodWindow>();
        _window.OnSelectOutfit += outfitId => SendMessage(new NetpodSelectOutfitMessage(outfitId));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is NetpodBoundUiState cast)
            _window?.UpdateState(cast);
    }
}
