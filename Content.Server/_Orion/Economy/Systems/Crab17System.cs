using Content.Server._Orion.Economy.Components;
using Content.Shared._Orion.Economy.Components;
using Content.Server._Orion.Mood;
using Content.Server.Popups;
using Content.Server.Chat.Systems;
using Content.Server.Station.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Server.Stack;
using Content.Shared.Stacks;
using Robust.Shared.Prototypes;
using Robust.Server.Audio;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;

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

    private static readonly ProtoId<StackPrototype> HolochipStackId = "CreditHolochip";
    private string? _pendingActivatorAccountId;

    public override void Initialize()
    {
        SubscribeLocalEvent<ProtocolCrab17PhoneComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<Crab17MarketComponent, InteractUsingEvent>(OnMarketInteractUsing);
        SubscribeLocalEvent<Crab17MarketComponent, MapInitEvent>(OnMarketMapInit);
        SubscribeLocalEvent<Crab17MarketComponent, ComponentShutdown>(OnMarketShutdown);
    }

    public override void Update(float frameTime)
    {
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<Crab17MarketComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (now >= comp.DeleteAt)
            {
                QueueDel(uid);
                continue;
            }

            if (now < comp.NextDrainTime)
                continue;

            comp.NextDrainTime = now + comp.DrainInterval;
            DrainTick((uid, comp));
        }
    }

    private void OnMarketShutdown(Entity<Crab17MarketComponent> ent, ref ComponentShutdown args)
    {
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

        if (_prototype.TryIndex(HolochipStackId, out var holo))
            _stack.Spawn(ent.Comp.StoredCredits, holo, Transform(ent).Coordinates);
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

        ent.Comp.Used = true;
        args.Handled = true;

        _audio.PlayPvs(ent.Comp.ActivateSound, ent);
        _popup.PopupEntity(Loc.GetString("protocol-crab17-activated"), ent, args.User, PopupType.LargeCaution);

        SpawnMarket(args.User,
            _bank.TryGetPlayerAccount(args.User, out _, out var account)
                ? account.AccountId
                : null,
            ent.Comp);
    }

    private void SpawnMarket(EntityUid user, string? activatorAccount, ProtocolCrab17PhoneComponent comp)
    {
        var coordinates = Transform(user).Coordinates;
        if (_station.GetOwningStation(user) is { } station)
        {
            var stationCoords = Transform(station).Coordinates;
            if (stationCoords.IsValid(EntityManager))
                coordinates = stationCoords;
        }

        Spawn(comp.LandingIndicatorPrototype, coordinates);

        _pendingActivatorAccountId = activatorAccount;
    }


    private void OnMarketMapInit(Entity<Crab17MarketComponent> ent, ref MapInitEvent args)
    {
        var areaName = ResolveAnnouncementArea(ent);
        _chat.DispatchStationAnnouncement(ent,
            Loc.GetString("protocol-crab17-announcement-start", ("area", areaName)),
            Loc.GetString("protocol-crab17-confirm-title"));

        ent.Comp.DeleteAt = _timing.CurTime + ent.Comp.LifeTime;
        ent.Comp.NextDrainTime = _timing.CurTime + ent.Comp.DrainInterval;
        ent.Comp.ActivatorAccountId = _pendingActivatorAccountId;
        _pendingActivatorAccountId = null;

        var query = EntityQueryEnumerator<StationAccountComponent>();
        while (query.MoveNext(out _, out var account))
        {
            if (account.Department != null)
                continue;

            if (!string.IsNullOrWhiteSpace(ent.Comp.ActivatorAccountId) && account.AccountId == ent.Comp.ActivatorAccountId)
                continue;

            account.BeingCrabbed = true;
            account.MoneyCrabbed = 0;
            account.CurrentCrab17Machine = ent;
        }
    }

    private void DrainTick(Entity<Crab17MarketComponent> market)
    {
        var hasTargets = false;
        var query = EntityQueryEnumerator<StationAccountComponent>();
        while (query.MoveNext(out var uid, out var account))
        {
            if (!account.BeingCrabbed || account.CurrentCrab17Machine != market.Owner)
                continue;

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

        if (!hasTargets)
            QueueDel(market.Owner);
    }

    private void OnMarketInteractUsing(Entity<Crab17MarketComponent> ent, ref InteractUsingEvent args)
    {
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

    private string ResolveAnnouncementArea(EntityUid source)
    {
        if (_station.GetOwningStation(source) is { } station)
            return MetaData(station).EntityName;

        return Loc.GetString("protocol-crab17-area-unknown");
    }
}
