using Content.Shared.Research.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Server._Orion.DocumentPrinter;

[RegisterComponent, NetworkedComponent]
public sealed partial class DocumentPrinterComponent : Component
{
    public List<(EntityUid, LatheRecipePrototype)> Queue { get; set; } = new();
    public SoundSpecifier SwitchSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");
    public bool IsOnAutocomplete = true;
}

public sealed class PrintingDocumentEvent : EntityEventArgs
{
    public EntityUid Paper { get; private set; }
    public EntityUid Actor { get; private set; }
    public PrintingDocumentEvent(EntityUid paper, EntityUid actor)
    {
        Paper = paper;
        Actor = actor;
    }
}
