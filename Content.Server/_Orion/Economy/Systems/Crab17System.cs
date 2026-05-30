using System.Globalization;
using System.Linq;
using Content.Server._Orion.Economy.Components;
using Content.Shared._Orion.Economy.Components;
using Content.Server._Orion.Mood;
using Content.Server.Cargo.Systems;
using Content.Server.Popups;
using Content.Server.Chat.Systems;
using Content.Server.Pinpointer;
using Content.Server.Respawn;
using Content.Server.Station.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Server.Stack;
using Content.Shared._Orion.Economy;
using Content.Shared.Cargo.Components;
using Content.Shared.Chat;
using Content.Shared.Destructible;
using Content.Shared.Stacks;
using Robust.Shared.Prototypes;
using Robust.Server.Audio;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Random.Helpers;
using Content.Shared.Station.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map;

namespace Content.Server._Orion.Economy.Systems;

public sealed class Crab17System : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly BankSystem _bank = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _sharedPopup = default!;
    [Dependency] private readonly StackSystem _stack = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly MoodSystem _mood = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SpecialRespawnSystem _respawn = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly CargoSystem _cargo = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;

    private static readonly ProtoId<StackPrototype> HolochipStackId = "CreditHolochip";
    private readonly Dictionary<EntityUid, (EntityUid ActivatorMind, string? ActivatorAccountId)> _pendingActivatorData = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<ProtocolCrab17PhoneComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<Crab17MarketComponent, InteractUsingEvent>(OnMarketInteractUsing);
        SubscribeLocalEvent<Crab17MarketComponent, MapInitEvent>(OnMarketMapInit);
        SubscribeLocalEvent<Crab17MarketComponent, ComponentShutdown>(OnMarketShutdown);
        SubscribeLocalEvent<Crab17MarketComponent, DestructionEventArgs>(OnMarketDestroyed);
        SubscribeLocalEvent<Crab17MarketComponent, EntityTerminatingEvent>(OnMarketTerminating);
    }

    public override void Update(float frameTime)
    {
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<Crab17MarketComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.DeleteAt != TimeSpan.Zero && now >= comp.DeleteAt)
            {
                QueueDel(uid);
                continue;
            }

            if (!comp.IsReady && now >= comp.StartupNextStageAt)
                AdvanceStartup((uid, comp));

            TryAnnounceBrag((uid, comp), now);

            if (now < comp.NextDrainTime)
                continue;

            comp.NextDrainTime = now + comp.DrainInterval;
            DrainTick((uid, comp));
        }
    }

    private void OnMarketShutdown(Entity<Crab17MarketComponent> ent, ref ComponentShutdown args)
    {
        FinalizeMarket(ent);
    }

    private void OnMarketDestroyed(Entity<Crab17MarketComponent> ent, ref DestructionEventArgs args)
    {
        FinalizeMarket(ent);
    }

    private void OnMarketTerminating(Entity<Crab17MarketComponent> ent, ref EntityTerminatingEvent args)
    {
        if (!ent.Comp.ShutdownHandled)
            _chat.DispatchStationAnnouncement(ent, Loc.GetString("protocol-crab17-announcement-stop"), Loc.GetString("protocol-crab17-confirm-title"));

        _pendingActivatorData.Remove(ent);
    }

    private void FinalizeMarket(Entity<Crab17MarketComponent> ent)
    {
        if (ent.Comp.ShutdownHandled)
            return;

        ent.Comp.ShutdownHandled = true;
        var query = EntityQueryEnumerator<StationAccountComponent>();
        while (query.MoveNext(out var uid, out var account))
        {
            if (account.CurrentCrab17Machine != ent.Owner)
                continue;

            StopDump((uid, account));
        }

        _chat.DispatchStationAnnouncement(ent, Loc.GetString("protocol-crab17-announcement-stop"), Loc.GetString("protocol-crab17-confirm-title"));

        if (ent.Comp.StoredCredits <= 0)
            return;

        var paid = false;
        if (!string.IsNullOrWhiteSpace(ent.Comp.ActivatorAccountId) && _bank.TryFindAccountById(ent.Comp.ActivatorAccountId, out var activator))
            paid = _bank.Deposit(activator, ent.Comp.StoredCredits, "?VIVA¿: !LA CRABBE¡", GetNetEntity(ent.Owner));

        if (paid)
            return;

        if (!_prototype.TryIndex(HolochipStackId, out var holo))
            return;

        var spawnCoordinates = _transform.GetMapCoordinates(ent);
        if (spawnCoordinates.MapId == MapId.Nullspace || !_mapSystem.MapExists(spawnCoordinates.MapId))
            return;

        var mapUid = Transform(ent).MapUid;
        if (mapUid != null && TerminatingOrDeleted(mapUid.Value))
            return;

        var holochip = Spawn(holo.Spawn, spawnCoordinates);
        _stack.SetCount(holochip, ent.Comp.StoredCredits);
    }

    private void OnUseInHand(Entity<ProtocolCrab17PhoneComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        var now = _timing.CurTime;
        if (ent.Comp.Used)
        {
            _popup.PopupEntity(Loc.GetString("protocol-crab17-already-used"), ent, args.User, PopupType.MediumCaution);
            args.Handled = true;
            return;
        }

        if (ent.Comp.PendingConfirmationUntil < now)
        {
            ent.Comp.PendingConfirmationUntil = now + ent.Comp.ConfirmationWindow;
            _popup.PopupEntity(Loc.GetString("protocol-crab17-confirm-message"), ent, args.User, PopupType.LargeCaution);
            args.Handled = true;
            return;
        }

        var spawned = SpawnMarket(args.User,
            _bank.TryGetPlayerAccount(args.User, out _, out var account)
                ? account.AccountId
                : null,
            ent.Comp);

        if (!spawned)
        {
            args.Handled = true;
            return;
        }

        ent.Comp.Used = true;
        args.Handled = true;

        _audio.PlayPvs(ent.Comp.ActivateSound, ent);
        _popup.PopupEntity(Loc.GetString("protocol-crab17-activated"), ent, args.User, PopupType.LargeCaution);
    }

    private bool SpawnMarket(EntityUid user, string? activatorAccount, ProtocolCrab17PhoneComponent comp)
    {
        if (!TryGetStationSpawnCoordinates(user, out var coordinates) && !TryGetAnyStationSpawnCoordinates(out coordinates))
        {
            _popup.PopupEntity(Loc.GetString("protocol-crab17-activation-error"), user, user, PopupType.MediumCaution);
            return false;
        }

        var market = EntityManager.CreateEntityUninitialized(comp.MarketPrototype, coordinates);
        _pendingActivatorData[market] = (user, activatorAccount);
        EntityManager.InitializeAndStartEntity(market);

        return true;
    }

    private void OnMarketMapInit(Entity<Crab17MarketComponent> ent, ref MapInitEvent args)
    {
        var areaName = ResolveAnnouncementArea(ent);
        _chat.DispatchStationAnnouncement(ent,
            Loc.GetString("protocol-crab17-announcement-start", ("area", areaName)),
            Loc.GetString("protocol-crab17-confirm-title"));

        ent.Comp.DeleteAt = _timing.CurTime + ent.Comp.LifeTime;
        ent.Comp.NextDrainTime = _timing.CurTime + ent.Comp.DrainInterval;
        ent.Comp.IsReady = false;
        ent.Comp.StartupStage = 0;
        ent.Comp.StartupNextStageAt = _timing.CurTime + TimeSpan.FromSeconds(0.35);
        ent.Comp.NextBragTime = _timing.CurTime + ent.Comp.BragInterval;
        _appearance.SetData(ent, Crab17Visuals.StartupStage, ent.Comp.StartupStage);

        if (!_pendingActivatorData.Remove(ent, out var activatorData))
            return;

        ent.Comp.ActivatorMind = activatorData.ActivatorMind;
        ent.Comp.ActivatorAccountId = activatorData.ActivatorAccountId;
    }

    private void DrainTick(Entity<Crab17MarketComponent> market)
    {
        var hasTargets = false;
        CleanupProtectedAccounts(market);

        var personalQuery = EntityQueryEnumerator<StationAccountComponent>();
        while (personalQuery.MoveNext(out var uid, out var account))
        {
            if (market.Comp.ActivatorMind != null && uid == market.Comp.ActivatorMind)
                continue;

            if (!string.IsNullOrWhiteSpace(market.Comp.ActivatorAccountId) && account.AccountId == market.Comp.ActivatorAccountId)
                continue;

            if (!string.IsNullOrWhiteSpace(account.AccountId) && market.Comp.ProtectedUntil.TryGetValue(account.AccountId, out var protectedUntil) && _timing.CurTime < protectedUntil)
                continue;

            if (!account.BeingCrabbed || account.CurrentCrab17Machine != market.Owner)
            {
                account.BeingCrabbed = true;

                if (account.MoneyCrabbed < 0)
                    account.MoneyCrabbed = 0;

                account.CurrentCrab17Machine = market.Owner;
            }

            var percent = _random.NextFloat(0.05f, 0.15f);
            var amount = (int) MathF.Round(account.Balance * percent);

            if (amount <= 0)
                continue;

            if (!_bank.Withdraw((uid, account), amount, "?VIVA¿: !LA CRABBE¡", GetNetEntity(market.Owner)))
                continue;

            hasTargets = true;
            account.MoneyCrabbed = SaturatingAdd(account.MoneyCrabbed, amount);
            market.Comp.StoredCredits = SaturatingAdd(market.Comp.StoredCredits, amount);
            market.Comp.CreditsSinceLastBrag = SaturatingAdd(market.Comp.CreditsSinceLastBrag, amount);
        }

        var stationQuery = EntityQueryEnumerator<StationBankAccountComponent>();
        while (stationQuery.MoveNext(out var stationUid, out var bankComp))
        {
            if (bankComp.Accounts.Count == 0)
                continue;

            foreach (var accountKey in bankComp.Accounts.Keys.ToList())
            {
                var balance = bankComp.Accounts[accountKey];
                if (balance <= 0)
                    continue;

                var percent = _random.NextFloat(0.02f, 0.08f);
                var amount = (int) MathF.Round(balance * percent);
                if (amount <= 0)
                    continue;

                _cargo.UpdateBankAccount((stationUid, bankComp), -amount, accountKey);
                var stolen = Math.Min(amount, balance);

                hasTargets = true;
                market.Comp.StoredCredits = SaturatingAdd(market.Comp.StoredCredits, stolen);
                market.Comp.CreditsSinceLastBrag = SaturatingAdd(market.Comp.CreditsSinceLastBrag, stolen);
            }
        }

        if (!hasTargets)
            QueueDel(market.Owner);
    }

    private void OnMarketInteractUsing(Entity<Crab17MarketComponent> ent, ref InteractUsingEvent args)
    {
        if (!ent.Comp.IsReady)
        {
            _sharedPopup.PopupEntity(Loc.GetString("protocol-crab17-not-ready"), ent, args.User, PopupType.MediumCaution);
            return;
        }

        if (!TryComp<IdCardComponent>(args.Used, out var id) || string.IsNullOrWhiteSpace(id.BankAccountId) || !_bank.TryFindAccountById(id.BankAccountId, out var account))
        {
            _sharedPopup.PopupEntity(Loc.GetString("protocol-crab17-card-no-account"), ent, args.User, PopupType.Medium);
            return;
        }

        if (!account.Comp.BeingCrabbed || account.Comp.CurrentCrab17Machine != ent.Owner)
        {
            _sharedPopup.PopupEntity(Loc.GetString("protocol-crab17-funds-already-safe"), ent, args.User, PopupType.Medium);
            return;
        }

        StopDump(account);
        ent.Comp.ProtectedUntil[id.BankAccountId] = _timing.CurTime + ent.Comp.ProtectionTtl;
        _sharedPopup.PopupEntity(Loc.GetString("protocol-crab17-funds-safe"), ent, args.User, PopupType.Medium);
    }

    private void StopDump(Entity<StationAccountComponent> account)
    {
        account.Comp.BeingCrabbed = false;
        account.Comp.CurrentCrab17Machine = null;

        if (account.Comp.MoneyCrabbed >= 10000 && TryComp<MindComponent>(account.Owner, out var mind) && mind.OwnedEntity is { } owned)
            _mood.AddEffect(owned, "LostMoneyCrab17");

        account.Comp.MoneyCrabbed = 0;
    }

    private void TryAnnounceBrag(Entity<Crab17MarketComponent> market, TimeSpan now)
    {
        if (now < market.Comp.NextBragTime)
            return;

        market.Comp.NextBragTime = now + market.Comp.BragInterval;

        if (market.Comp.CreditsSinceLastBrag <= 0)
            return;

        var amount = market.Comp.CreditsSinceLastBrag.ToString("N0", CultureInfo.InvariantCulture);
        if (!_prototype.TryIndex(market.Comp.BragPhraseDataset, out var phraseDataset))
            return;

        var phrase = _random.Pick(phraseDataset);

        _chat.TrySendInGameICMessage(market, Loc.GetString("protocol-crab17-brag-announcement", ("credits", amount), ("line", phrase)), InGameICChatType.Speak, hideChat: false);

        market.Comp.CreditsSinceLastBrag = 0;
    }

    private static int SaturatingAdd(int a, int b)
    {
        var sum = (long) a + b;
        return (int) Math.Clamp(sum, int.MinValue, int.MaxValue);
    }

    private void AdvanceStartup(Entity<Crab17MarketComponent> ent)
    {
        switch (ent.Comp.StartupStage)
        {
            case 0:
            case 1:
                _audio.PlayPvs("/Audio/Items/pen_click.ogg", ent);
                ent.Comp.StartupNextStageAt = _timing.CurTime + TimeSpan.FromSeconds(0.35);
                break;
            case 2:
                _audio.PlayPvs("/Audio/_Orion/Machines/twobeep_high.ogg", ent);
                ent.Comp.StartupNextStageAt = _timing.CurTime + TimeSpan.FromSeconds(0.45);
                break;
            case 3:
                ent.Comp.StartupNextStageAt = _timing.CurTime + TimeSpan.FromSeconds(0.35);
                break;
            case 4:
            case 5:
                ent.Comp.StartupNextStageAt = _timing.CurTime + TimeSpan.FromSeconds(0.25);
                break;
            case 6:
                _audio.PlayPvs("/Audio/Machines/beep.ogg", ent);
                ent.Comp.IsReady = true;
                break;
        }

        ent.Comp.StartupStage++;
        _appearance.SetData(ent, Crab17Visuals.StartupStage, ent.Comp.StartupStage);
    }

    private string ResolveAnnouncementArea(EntityUid source)
    {
        if (_navMap.TryGetNearestBeacon((source, Transform(source)), out var beacon, out _) && beacon?.Comp.Text is { Length: > 0 } markerName)
            return markerName;

        return Loc.GetString("protocol-crab17-area-unknown");
    }

    private bool TryGetStationSpawnCoordinates(EntityUid user, out EntityCoordinates coordinates)
    {
        coordinates = EntityCoordinates.Invalid;

        if (_station.GetOwningStation(user) is not { } stationUid)
            return false;

        return TryGetStationSpawnCoordinatesForStation(stationUid, out coordinates);
    }

    private bool TryGetAnyStationSpawnCoordinates(out EntityCoordinates coordinates)
    {
        coordinates = EntityCoordinates.Invalid;

        var stations = _station.GetStations();
        if (stations.Count == 0)
            return false;

        var shuffledStations = stations.ToList();
        _random.Shuffle(shuffledStations);

        foreach (var stationUid in shuffledStations)
        {
            if (!TryGetStationSpawnCoordinatesForStation(stationUid, out coordinates))
                continue;

            return true;
        }

        return false;
    }

    private bool TryGetStationSpawnCoordinatesForStation(EntityUid stationUid, out EntityCoordinates coordinates)
    {
        coordinates = EntityCoordinates.Invalid;

        if (!TryComp<StationDataComponent>(stationUid, out var stationData))
            return false;

        var targetGrid = _station.GetLargestGrid(stationUid);
        if (targetGrid == null && stationData.Grids.Count > 0)
            targetGrid = _random.Pick(stationData.Grids);

        if (targetGrid == null)
            return false;

        var mapUid = Transform(targetGrid.Value).MapUid;
        if (mapUid == null)
            return false;

        return _respawn.TryFindRandomTile(targetGrid.Value, mapUid.Value, 60, out coordinates);
    }

    private void CleanupProtectedAccounts(Entity<Crab17MarketComponent> market)
    {
        if (market.Comp.ProtectedUntil.Count == 0)
            return;

        var now = _timing.CurTime;
        foreach (var account in market.Comp.ProtectedUntil.Where(entry => entry.Value <= now).ToList())
        {
            market.Comp.ProtectedUntil.Remove(account.Key);
        }
    }
}
