using Content.Client.UserInterface.Controls;
using Content.Shared._Orion.CorticalBorer;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Orion.CorticalBorer;

[UsedImplicitly]
public sealed class CorticalBorerForceSpeakBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private CorticalBorerForceSpeakWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<CorticalBorerForceSpeakWindow>();
        _window.SetInfoFromEntity(EntMan, Owner);
        _window.OnForceSpeakButtonPressed += msg =>
        {
            SendMessage(new CorticalBorerForceSpeakMessage(msg));
            _window?.Close();
        };
    }
}
