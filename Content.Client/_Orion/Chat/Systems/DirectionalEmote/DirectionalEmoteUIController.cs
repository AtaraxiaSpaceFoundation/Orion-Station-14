using Content.Client.Chat.TypingIndicator;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client._Orion.Chat.Systems.DirectionalEmote;

[UsedImplicitly]
public sealed class DirectionalEmoteUIController : UIController
{
    [UISystemDependency] private readonly DirectionalEmoteSystem _directionalEmoteSystem = default!;
    [UISystemDependency] private readonly TypingIndicatorSystem _typingIndicator = default!;

    private DirectionalEmoteWindow _emoteWindow = default!;

    public void OpenWindow()
    {
        EnsureWindow();

        _emoteWindow.OpenCentered();
        _emoteWindow.MoveToFront();

        _typingIndicator.ClientChangedWindowStatus(true);

        _emoteWindow.MessageChanged += () =>
        {
            _typingIndicator.ClientChangedChatText();
            _typingIndicator.ClientChangedWindowStatus(true);
        };

        _emoteWindow.AcceptPressed += () =>
        {
            _typingIndicator.ClientChangedWindowStatus(false);
            _typingIndicator.ClientSubmittedChatText();
            _directionalEmoteSystem.ShowMessage(_emoteWindow.Source, _emoteWindow.Target, _emoteWindow.Text);
            _emoteWindow.Dispose();
        };
    }

    private void EnsureWindow()
    {
        if (_emoteWindow is { Disposed: false })
            return;

        _emoteWindow = UIManager.CreateWindow<DirectionalEmoteWindow>();
    }

    public void ToggleWindow(NetEntity source, NetEntity emoteTarget)
    {
        EnsureWindow();
        _emoteWindow.Source = source;
        _emoteWindow.Target = emoteTarget;

        if (_emoteWindow.IsOpen)
        {
            _typingIndicator.ClientChangedWindowStatus(false);
            _emoteWindow.Dispose();
        }
        else
        {
            OpenWindow();
        }
    }
}
