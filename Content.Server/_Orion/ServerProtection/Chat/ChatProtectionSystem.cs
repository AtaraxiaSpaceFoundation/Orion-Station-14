using System.Net;
using System.Net.Sockets;
using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.Administration.Systems;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Hands.Systems;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.StationRecords.Systems;
using Content.Shared._Orion.ServerProtection.Chat;
using Content.Shared.Administration.Managers;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Forensics.Components;
using Content.Shared.Hands.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.Popups;
using Content.Shared.StationRecords;
using Content.Shared.Throwing;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Orion.ServerProtection.Chat;

//
// License-Identifier: AGPL-3.0-or-later
//

public sealed class ChatProtectionSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IBanManager _banManager = default!;
    [Dependency] private readonly IPlayerLocator _locator = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly ISharedAdminManager _admin = default!;
    [Dependency] private readonly IChatManager _chat = default!;

    private ISawmill _log = default!;
    private readonly List<ChatProtectionListPrototype> _index = new();
    private readonly HashSet<string> _icWords = new();
    private readonly HashSet<string> _oocWords = new();
    private bool _enabled = false;
    private bool _cacheDone = false;

    private HandsSystem Hands => EntityManager.System<HandsSystem>();
    private InventorySystem Inventory => EntityManager.System<InventorySystem>();
    private MindSystem Minds => EntityManager.System<MindSystem>();
    private PhysicsSystem Physics => EntityManager.System<PhysicsSystem>();
    private PopupSystem Popup => EntityManager.System<PopupSystem>();
    private GameTicker GameTicker => EntityManager.System<GameTicker>();
    private SharedAudioSystem Audio => EntityManager.System<SharedAudioSystem>();
    private StationRecordsSystem StationRecords => EntityManager.System<StationRecordsSystem>();
    private new TransformSystem Transform => EntityManager.System<TransformSystem>();

    public override void Initialize()
    {
        base.Initialize();

        _log = Logger.GetSawmill("serverprotection.chat_protection");
        _proto.PrototypesReloaded += OnPrototypesReloaded;
        _cfg.OnValueChanged(CCVars.ChatProtectionEnabled, SetEnabled, true);
    }


    private void SetEnabled(bool value)
    {
        _enabled = value;
    }

    private void CachePrototypes()
    {
        _index.Clear();
        _icWords.Clear();
        _oocWords.Clear();

        foreach (var proto in _proto.EnumeratePrototypes<ChatProtectionListPrototype>())
        {
            _index.Add(proto);

            switch (proto.ID) // Handled by "Prototypes/_Orion/chat_protection.yml"
            {
                case "IC_BannedWords":
                    foreach (var word in proto.Words)
                    {
                        _icWords.Add(word);
                    }

                    break;

                case "OOC_BannedWords":
                    foreach (var word in proto.Words)
                    {
                        _oocWords.Add(word);
                    }

                    break;
            }
        }

        _log.Info($"Кэшировано {_icWords.Count} IC и {_oocWords.Count} OOC запрещённых слов.");
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        CachePrototypes();
    }

    public bool CheckICMessage(string message, EntityUid player)
    {
        if (!_enabled || string.IsNullOrEmpty(message))
            return false;

        if (!TryGetSession(player, out var session))
            return false;

        if (session == null)
            return false;

        if (_admin.IsAdmin(player, true))
           return false;

        if (!_cacheDone) // Something like initalization for prototypes
            CachePrototypes();

        foreach (var word in _icWords)
        {
            if (!message.Contains(word, StringComparison.OrdinalIgnoreCase))
                continue;

            HandleViolation(session, word, "IC");
            return true;
        }

        _cacheDone = true;

        return false;
    }

    public bool CheckOOCMessage(string message, ICommonSession session)
    {
        if (!_enabled || string.IsNullOrEmpty(message))
            return false;

        if (_admin.IsAdmin(session, true))
            return false;

        if (!_cacheDone) // Something like initalization for prototypes
            CachePrototypes();

        foreach (var word in _oocWords)
        {
            if (!message.Contains(word, StringComparison.OrdinalIgnoreCase))
                continue;

            HandleViolation(session, word, "OOC");
            return true;
        }

        _cacheDone = true;

        return false;
    }

    private bool TryGetSession(EntityUid player, out ICommonSession? session)
    {
        session = null;
        return _playerManager.TryGetSessionByEntity(player, out session);
    }

    private void HandleViolation(ICommonSession player, string word, string channel)
    {
        var reason = Loc.GetString("chat-protection-ban-reason", ("word", word), ("channel", channel));
        _log.Info($"{player.Name} ({player.UserId}) использовал запрещённое слово: '{word}' в {channel}");

        switch (channel)
        {
            case "IC": // Erase them from universe and ban
            {
                _chat.DeleteMessagesBy(player.UserId);

                var eraseEvent = new EraseEvent(player.UserId);

                if (!Minds.TryGetMind(player.UserId, out var mindId, out var mind) || mind.OwnedEntity == null || TerminatingOrDeleted(mind.OwnedEntity.Value))
                {
                    RaiseLocalEvent(ref eraseEvent);
                    return;
                }

                var entity = mind.OwnedEntity.Value;

                if (TryComp(entity, out TransformComponent? transform))
                {
                    var coordinates = Transform.GetMoverCoordinates(entity, transform);
                    var name = Identity.Entity(entity, EntityManager);
                    Popup.PopupCoordinates(Loc.GetString("admin-erase-popup", ("user", name)), coordinates, PopupType.LargeCaution);
                    var filter = Filter.Pvs(coordinates, 1, EntityManager, _playerManager);
                    var audioParams = new AudioParams().WithVolume(3);
                    Audio.PlayStatic("/Audio/Effects/pop_high.ogg", filter, coordinates, true, audioParams);
                }

                foreach (var item in Inventory.GetHandOrInventoryEntities(entity))
                {
                    if (TryComp(item, out PdaComponent? pda) &&
                        TryComp(pda.ContainedId, out StationRecordKeyStorageComponent? keyStorage) &&
                        keyStorage.Key is { } key &&
                        StationRecords.TryGetRecord(key, out GeneralStationRecord? record))
                    {
                        if (TryComp(entity, out DnaComponent? dna) &&
                            dna.DNA != record.DNA)
                        {
                            continue;
                        }

                        if (TryComp(entity, out FingerprintComponent? fingerPrint) &&
                            fingerPrint.Fingerprint != record.Fingerprint)
                        {
                            continue;
                        }

                        StationRecords.RemoveRecord(key);
                        Del(item);
                    }
                }

                if (Inventory.TryGetContainerSlotEnumerator(entity, out var enumerator))
                {
                    while (enumerator.NextItem(out var item, out var slot))
                    {
                        if (Inventory.TryUnequip(entity, entity, slot.Name, true, true))
                            Physics.ApplyAngularImpulse(item, ThrowingSystem.ThrowAngularImpulse);
                    }
                }

                if (TryComp(entity, out HandsComponent? hands))
                {
                    foreach (var hand in Hands.EnumerateHands((entity, hands)))
                    {
                        Hands.TryDrop((entity, hands), hand, checkActionBlocker: false, doDropInteraction: false);
                    }
                }

                Minds.WipeMind(mindId, mind);
                QueueDel(entity);

                if (_playerManager.TryGetSessionById(player.UserId, out var session))
                    GameTicker.SpawnObserver(session);

                RaiseLocalEvent(ref eraseEvent);

                _chat.SendAdminAlert(Loc.GetString("chat-protection-admin-announcement-ban-reason", ("player", player.Name), ("word", word), ("channel", channel)));
                ApplyBan(player, reason);
                break;
            }

            case "OOC": // 把他放逐就行了。😡😡😡
            {
                _chat.DeleteMessagesBy(player.UserId);
                _chat.SendAdminAlert(Loc.GetString("chat-protection-admin-announcement-ban-reason", ("player", player.Name), ("word", word), ("channel", channel)));
                ApplyBan(player, reason);
                break;
            }
        }
    }

    private async void ApplyBan(ICommonSession player, string reason)
    {
        (IPAddress, int)? targetIP = null;
        ImmutableTypedHwid? targetHWid = null;

        var sessionData = await _locator.LookupIdAsync(player.UserId);
        if (sessionData != null)
        {
            if (sessionData.LastAddress is not null)
            {
                var prefix = sessionData.LastAddress.AddressFamily == AddressFamily.InterNetwork ? 32 : 64;
                targetIP = (sessionData.LastAddress, prefix);
            }

            targetHWid = sessionData.LastHWId;
        }

        _banManager.CreateServerBan(
            player.UserId,
            player.Name,
            null,
            targetIP,
            targetHWid,
            0, // Permanent
            NoteSeverity.High,
            $"{reason}"
        );
    }
}
