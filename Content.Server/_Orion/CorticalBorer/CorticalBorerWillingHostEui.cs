using Content.Server.EUI;
using Content.Shared._Orion.CorticalBorer;
using Content.Shared.Eui;
using Content.Shared._Orion.CorticalBorer.Components;

namespace Content.Server._Orion.CorticalBorer;

public sealed class CorticalBorerWillingHostEui(
    Entity<CorticalBorerComponent> borer,
    EntityUid host,
    CorticalBorerSystem system) : BaseEui
{
    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not CorticalBorerWillingHostChoiceMessage choice)
            return;

        system.HandleWillingHostChoice(borer, host, choice.Accepted);
        Close();
    }
}
