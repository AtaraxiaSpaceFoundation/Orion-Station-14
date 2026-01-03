using Content.Server._Orion.Chat.Systems;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Server._Orion.Chat.Components;

/// <summary>
/// Dispatches a radio announcement when the entity is mapinit'd.
/// </summary>
[RegisterComponent, Access(typeof(RadioAnnounceOnSpawnSystem))]
public sealed partial class RadioAnnounceOnSpawnComponent : Component
{
    /// <summary>
    ///     Radio channels to announce on.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<RadioChannelPrototype>[] AnnouncementChannels = default!;

    /// <summary>
    ///     Locale id of the announcement message.
    /// </summary>
    [DataField(required: true)]
    public LocId Message;

    /// <summary>
    ///     Locale id of the announcement's sender name.
    /// </summary>
    [DataField]
    public LocId Sender = "automatic-notification-system-sender";
}
