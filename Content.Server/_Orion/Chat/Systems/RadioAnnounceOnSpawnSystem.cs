using Content.Server._Orion.Chat.Components;
using Content.Server.Radio.Components;
using Content.Server.Radio.EntitySystems;
using Robust.Shared.Map;

namespace Content.Server._Orion.Chat.Systems;

public sealed class RadioAnnounceOnSpawnSystem : EntitySystem
{
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RadioAnnounceOnSpawnComponent, MapInitEvent>(OnInit);
    }

    private void OnInit(EntityUid uid, RadioAnnounceOnSpawnComponent comp, MapInitEvent args)
    {
        var entityName = MetaData(uid).EntityName;
        var message = Loc.GetString(comp.Message, ("name", entityName));

        var componentName = Loc.GetString(comp.Sender);
        var sender = Spawn(null, MapCoordinates.Nullspace);

        _metaData.SetEntityName(sender, componentName); // Rename spawned entity

        EntityManager.AddComponent<RadioMicrophoneComponent>(sender);

        foreach (var channelId in comp.AnnouncementChannels)
        {
            _radio.SendRadioMessage(sender, message, channelId, sender, escapeMarkup: false);
        }

        EntityManager.QueueDeleteEntity(sender);
    }
}
