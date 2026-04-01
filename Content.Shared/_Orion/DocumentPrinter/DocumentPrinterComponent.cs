using Content.Shared.Research.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared._Orion.DocumentPrinter;

[RegisterComponent]
public sealed partial class DocumentPrinterComponent : Component
{
    [DataField]
    public List<(EntityUid, LatheRecipePrototype)> Queue { get; set; } = new();

    [DataField]
    public SoundSpecifier SwitchSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

    [DataField]
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
