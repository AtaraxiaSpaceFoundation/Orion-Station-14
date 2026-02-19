using Content.Client.Eui;
using Content.Shared._Orion.CorticalBorer;
using JetBrains.Annotations;
using Robust.Client.Graphics;

namespace Content.Client._Orion.CorticalBorer;

[UsedImplicitly]
public sealed class CorticalBorerWillingHostEui : BaseEui
{
    private readonly CorticalBorerWillingHostWindow _window = new();
    private bool _responded;

    public CorticalBorerWillingHostEui()
    {
        _window.AcceptButton.OnPressed += _ =>
        {
            if (_responded)
                return;

            SendMessage(new CorticalBorerWillingHostChoiceMessage(true));
            _window.Close();
        };

        _window.DenyButton.OnPressed += _ =>
        {
            if (_responded)
                return;

            _responded = true;
            SendMessage(new CorticalBorerWillingHostChoiceMessage(false));
            _window.Close();
        };

        _window.OnClose += () =>
        {
            if (_responded)
                return;

            _responded = true;
            SendMessage(new CorticalBorerWillingHostChoiceMessage(false));
        };
    }

    public override void Opened()
    {
        IoCManager.Resolve<IClyde>().RequestWindowAttention();
        _window.OpenCentered();
    }

    public override void Closed()
    {
        _window.Close();
    }
}
