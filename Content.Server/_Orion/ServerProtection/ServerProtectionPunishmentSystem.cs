using System.Net;
using System.Net.Sockets;
using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.Administration.Systems;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.GhostKick;
using Content.Server.Hands.Systems;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.StationRecords.Systems;
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
using Robust.Shared.Player;

namespace Content.Server._Orion.ServerProtection;

//
// License-Identifier: AGPL-3.0-or-later
//

public sealed class ServerProtectionPunishmentSystem : EntitySystem
{
    [Dependency] private readonly IBanManager _banManager = default!;
    [Dependency] private readonly IPlayerLocator _locator = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly GhostKickManager _ghostKickManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private ISawmill _log = default!;

    #region Dont Mind About This
    private HandsSystem Hands => EntityManager.System<HandsSystem>();
    private InventorySystem Inventory => EntityManager.System<InventorySystem>();
    private MindSystem Minds => EntityManager.System<MindSystem>();
    private PhysicsSystem Physics => EntityManager.System<PhysicsSystem>();
    private PopupSystem Popup => EntityManager.System<PopupSystem>();
    private GameTicker GameTicker => EntityManager.System<GameTicker>();
    private SharedAudioSystem Audio => EntityManager.System<SharedAudioSystem>();
    private StationRecordsSystem StationRecords => EntityManager.System<StationRecordsSystem>();
    private new TransformSystem Transform => EntityManager.System<TransformSystem>();
    #endregion

    public override void Initialize()
    {
        base.Initialize();
        _log = Logger.GetSawmill("serverprotection.actions");
    }

    /// <summary>
    /// Applies a ban to the player.
    /// </summary>
    public async void ApplyBan(ICommonSession player, string reason, int durationSeconds = 0)
    {
        if (string.IsNullOrWhiteSpace(player.Name))
            return;

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

        uint? expires = durationSeconds <= 0 ? null : (uint)durationSeconds;

        _banManager.CreateServerBan(
            player.UserId,
            player.Name,
            null,
            targetIP,
            targetHWid,
            expires,
            NoteSeverity.High,
            $"{reason}"
        );

        _log.Info($"{player.Name} был забанен: {reason}");
    }

    /// <summary>
    /// Kicks the player.
    /// </summary>
    public void KickPlayer(ICommonSession player, string reason)
    {
        _ghostKickManager.DoDisconnect(player.Channel, reason);
        _log.Info($"{player.Name} был кикнут: {reason}");
    }

    /// <summary>
    /// Deletes all messages sent by the player.
    /// </summary>
    public void DeleteMessages(ICommonSession player)
    {
        _chat.DeleteMessagesBy(player.UserId);
    }

    /// <summary>
    /// Erases the character (drops inventory, destroys entity, wipes mind).
    /// </summary>
    public async void EraseCharacter(ICommonSession player)
    {
        if (!Minds.TryGetMind(player.UserId, out var mindId, out var mind) ||
            mind.OwnedEntity == null ||
            TerminatingOrDeleted(mind.OwnedEntity.Value))
        {
            var eraseEvent = new EraseEvent(player.UserId);
            RaiseLocalEvent(ref eraseEvent);
            return;
        }

        var entity = mind.OwnedEntity.Value;
        var eraseEventLocal = new EraseEvent(player.UserId);

        if (TryComp(entity, out TransformComponent? transform))
        {
            var coordinates = Transform.GetMoverCoordinates(entity, transform);
            var name = Identity.Entity(entity, EntityManager);
            Popup.PopupCoordinates(Loc.GetString("admin-erase-popup", ("user", name)),
                coordinates,
                PopupType.LargeCaution);
            var filter = Filter.Pvs(coordinates, 1, EntityManager, _playerManager);
            var audioParams = new AudioParams().WithVolume(3);
            Audio.PlayStatic("/Audio/Effects/pop_high.ogg", filter, coordinates, true, audioParams);
        }

        foreach (var item in Inventory.GetHandOrInventoryEntities(entity))
        {
            if (!TryComp(item, out PdaComponent? pda) ||
                !TryComp(pda.ContainedId, out StationRecordKeyStorageComponent? keyStorage) ||
                keyStorage.Key is not { } key ||
                !StationRecords.TryGetRecord(key, out GeneralStationRecord? record))
                continue;

            if (TryComp(entity, out DnaComponent? dna) &&
                dna.DNA != record.DNA)
                continue;

            if (TryComp(entity, out FingerprintComponent? fingerprint) &&
                fingerprint.Fingerprint != record.Fingerprint)
                continue;

            StationRecords.RemoveRecord(key);
            Del(item);
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

        RaiseLocalEvent(ref eraseEventLocal);
    }

    /// <summary>
    /// Sends an admin alert.
    /// </summary>
    public void SendAdminAlert(string message)
    {
        _chat.SendAdminAlert(message);
    }
}
