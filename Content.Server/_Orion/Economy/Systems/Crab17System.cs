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
using Content.Shared.Stacks;
using Robust.Shared.Prototypes;
using Robust.Server.Audio;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
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

    private static readonly ProtoId<StackPrototype> HolochipStackId = "CreditHolochip";
    private string? _pendingActivatorAccountId;

    public override void Initialize()
    {
        SubscribeLocalEvent<ProtocolCrab17PhoneComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<Crab17MarketComponent, InteractUsingEvent>(OnMarketInteractUsing);
        SubscribeLocalEvent<Crab17MarketComponent, MapInitEvent>(OnMarketMapInit);
        SubscribeLocalEvent<Crab17MarketComponent, ComponentShutdown>(OnMarketShutdown);
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

    private void OnMarketTerminating(Entity<Crab17MarketComponent> ent, ref EntityTerminatingEvent args)
    {
        _chat.DispatchStationAnnouncement(ent, Loc.GetString("protocol-crab17-announcement-stop"), Loc.GetString("protocol-crab17-confirm-title"));
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

        if (!_prototype.TryIndex(HolochipStackId, out var holo))
            return;

        var mapCoordinates = _transform.GetMapCoordinates(ent);
        var holochip = Spawn(holo.Spawn, mapCoordinates);
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

        _pendingActivatorAccountId = activatorAccount;
        Spawn(comp.MarketPrototype, coordinates);
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
        _appearance.SetData(ent, Crab17Visuals.StartupStage, ent.Comp.StartupStage);
        ent.Comp.ActivatorAccountId = _pendingActivatorAccountId;
        _pendingActivatorAccountId = null;
    }

    private void DrainTick(Entity<Crab17MarketComponent> market)
    {
        var hasTargets = false;

        var personalQuery = EntityQueryEnumerator<StationAccountComponent>();
        while (personalQuery.MoveNext(out var uid, out var account))
        {
            if (!string.IsNullOrWhiteSpace(market.Comp.ActivatorAccountId) && account.AccountId == market.Comp.ActivatorAccountId)
                continue;

            if (!account.BeingCrabbed || account.CurrentCrab17Machine != market.Owner)
            {
                account.BeingCrabbed = true;
                account.MoneyCrabbed = 0;
                account.CurrentCrab17Machine = market.Owner;
            }

            hasTargets = true;
            var percent = _random.NextFloat(0.05f, 0.15f);
            var amount = (int) MathF.Round(account.Balance * percent);

            if (amount <= 0)
                continue;

            if (!_bank.Withdraw((uid, account), amount, "?VIVA¿: !LA CRABBE¡", GetNetEntity(market.Owner)))
                continue;

            account.MoneyCrabbed += amount;

            if (market.Comp.ActivatorAccountId != null && _bank.TryFindAccountById(market.Comp.ActivatorAccountId, out var activator))
                _bank.Deposit(activator, amount, "?VIVA¿: !LA CRABBE¡", GetNetEntity(uid));
            else
                market.Comp.StoredCredits += amount;
        }

        var stationQuery = EntityQueryEnumerator<StationBankAccountComponent>();
        while (stationQuery.MoveNext(out var stationUid, out var bankComp))
        {
            if (bankComp.Accounts.Count == 0)
                continue;

            hasTargets = true;
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

                if (market.Comp.ActivatorAccountId != null && _bank.TryFindAccountById(market.Comp.ActivatorAccountId, out var activatorDept))
                    _bank.Deposit(activatorDept, amount, "?VIVA¿: !LA CRABBE¡", GetNetEntity(stationUid));
                else
                    market.Comp.StoredCredits += amount;
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
        _sharedPopup.PopupEntity(Loc.GetString("protocol-crab17-funds-safe"), ent, args.User, PopupType.Medium);
    }

    private void StopDump(Entity<StationAccountComponent> account)
    {
        account.Comp.BeingCrabbed = false;
        account.Comp.CurrentCrab17Machine = null;

        if (account.Comp.MoneyCrabbed >= 10000 && TryComp<MindComponent>(account.Owner, out var mind) && mind.OwnedEntity is { } owned)
            _mood.AddEffect(owned, "LostMoneyCrab17");
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
}
