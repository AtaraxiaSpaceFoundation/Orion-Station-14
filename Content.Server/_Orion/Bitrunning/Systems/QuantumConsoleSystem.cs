using Content.Shared._Orion.Bitrunning;
using Content.Shared._Orion.Bitrunning.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Emag.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server._Orion.Bitrunning.Systems;

public sealed class QuantumConsoleSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly QuantumServerSystem _server = default!;
    [Dependency] private readonly BitrunningDomainSystem _domains = default!;
    [Dependency] private readonly SharedDeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly TimeSpan UiRefresh = TimeSpan.FromSeconds(1);
    private const string ServerSinkPort = "BitrunningConsoleSink";
    private TimeSpan _nextRefresh;

    public override void Initialize()
    {
        SubscribeLocalEvent<QuantumConsoleComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<QuantumConsoleComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<QuantumConsoleComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<QuantumConsoleComponent, QuantumConsoleLoadDomainMessage>(OnLoadDomain);
        SubscribeLocalEvent<QuantumConsoleComponent, QuantumConsoleRandomDomainMessage>(OnRandomDomain);
        SubscribeLocalEvent<QuantumConsoleComponent, QuantumConsoleStopDomainMessage>(OnStopDomain);
        SubscribeLocalEvent<QuantumConsoleComponent, QuantumConsoleRefreshMessage>(OnRefresh);
        SubscribeLocalEvent<QuantumConsoleComponent, QuantumConsoleBroadcastMessage>(OnBroadcast);
        SubscribeLocalEvent<QuantumConsoleComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<QuantumConsoleComponent, PortDisconnectedEvent>(OnPortDisconnected);
    }

    private void OnInit(Entity<QuantumConsoleComponent> ent, ref ComponentInit args)
    {
        _deviceLink.EnsureSinkPorts(ent.Owner, ServerSinkPort);
    }

    private void OnMapInit(Entity<QuantumConsoleComponent> ent, ref MapInitEvent args)
    {
        RefreshLinkedServer(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextRefresh)
            return;

        _nextRefresh = _timing.CurTime + UiRefresh;
        var query = EntityQueryEnumerator<QuantumConsoleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!_ui.IsUiOpen(uid, QuantumConsoleUiKey.Key))
                continue;

            UpdateUi((uid, comp));
        }
    }

    private void OnUiOpened(Entity<QuantumConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUi(ent);
    }

    private void OnLoadDomain(Entity<QuantumConsoleComponent> ent, ref QuantumConsoleLoadDomainMessage args)
    {
        var server = FindServer(ent);
        if (server == null)
            return;

        _server.TryColdBoot(server.Value, args.DomainId);
        UpdateUi(ent);
    }

    private void OnRandomDomain(Entity<QuantumConsoleComponent> ent, ref QuantumConsoleRandomDomainMessage args)
    {
        var server = FindServer(ent);
        if (server == null)
            return;

        var domain = _server.GetRandomDomainId(server.Value);
        if (domain == null)
            return;

        _server.TryColdBoot(server.Value, domain, true);
        UpdateUi(ent);
    }

    private void OnStopDomain(Entity<QuantumConsoleComponent> ent, ref QuantumConsoleStopDomainMessage args)
    {
        var serverUid = FindServer(ent);
        if (serverUid == null || !TryComp<QuantumServerComponent>(serverUid, out var serverComp))
            return;

        _server.StopDomain((serverUid.Value, serverComp));
        UpdateUi(ent);
    }

    private void OnRefresh(Entity<QuantumConsoleComponent> ent, ref QuantumConsoleRefreshMessage args)
    {
        UpdateUi(ent);
    }

    private void OnBroadcast(Entity<QuantumConsoleComponent> ent, ref QuantumConsoleBroadcastMessage args)
    {
        var serverUid = FindServer(ent);
        if (serverUid == null || !HasComp<QuantumServerComponent>(serverUid))
            return;

        _server.SetBroadcastState(serverUid.Value, args.Enabled);
        UpdateUi(ent);
    }

    private void UpdateUi(Entity<QuantumConsoleComponent> ent)
    {
        var serverUid = FindServer(ent);
        if (serverUid == null || !TryComp<QuantumServerComponent>(serverUid, out var server))
        {
            _ui.SetUiState(ent.Owner, QuantumConsoleUiKey.Key, new QuantumConsoleBoundUiState(false, null, null, 0, 0, 0, BitrunningServerState.Ready, false, 0f, 0f, new List<BitrunningDomainListing>(), new List<BitrunningOccupantListing>()));
            return;
        }

        var domains = new List<BitrunningDomainListing>();
        var emagged = HasComp<EmaggedComponent>(serverUid.Value);
        foreach (var domain in _domains.GetAllDomains())
        {
            if (domain.Difficulty == BitrunningDifficulty.Extreme && !emagged)
                continue;

            domains.Add(new BitrunningDomainListing(
                domain.ID,
                _domains.GetDisplayName(domain, server.ScannerTier, server.Points),
                _domains.GetDisplayDescription(domain, server.ScannerTier, server.Points),
                domain.Cost,
                _domains.GetDisplayReward(domain, server.ScannerTier, server.Points),
                domain.Difficulty,
                domain.IsModular,
                domain.HasSecondaryObjectives));
        }

        var occupants = new List<BitrunningOccupantListing>();
        foreach (var uid in server.Occupants)
        {
            if (!Exists(uid))
                continue;

            var name = Name(uid);
            var noHit = CompOrNull<AvatarConnectionComponent>(uid)?.NoHit ?? false;
            occupants.Add(new BitrunningOccupantListing(name, noHit));
        }

        var cooldownTotal = (float) server.Cooldown.TotalSeconds;
        var cooldownRemaining = Math.Max(0f, (float) (server.CooldownEndTime - _timing.CurTime).TotalSeconds);

        _ui.SetUiState(ent.Owner, QuantumConsoleUiKey.Key, new QuantumConsoleBoundUiState(true, GetNetEntity(serverUid.Value), server.CurrentDomain, server.Occupants.Count, server.Points, server.ScannerTier, server.State, server.BroadcastEnabled, cooldownTotal, cooldownRemaining, domains, occupants));
    }

    private EntityUid? FindServer(Entity<QuantumConsoleComponent> ent)
    {
        if (ent.Comp.LinkedServerId is { } linkedUid && Exists(linkedUid) && HasComp<QuantumServerComponent>(linkedUid))
            return linkedUid;

        RefreshLinkedServer(ent);
        return ent.Comp.LinkedServerId;
    }

    private void OnNewLink(Entity<QuantumConsoleComponent> ent, ref NewLinkEvent args)
    {
        if (args.Sink != ent.Owner || args.SinkPort != ServerSinkPort)
            return;

        if (!HasComp<QuantumServerComponent>(args.Source))
            return;

        ent.Comp.LinkedServerId = args.Source;
    }

    private static void OnPortDisconnected(Entity<QuantumConsoleComponent> ent, ref PortDisconnectedEvent args)
    {
        if (args.Port != ServerSinkPort || ent.Comp.LinkedServerId != args.RemovedPortUid)
            return;

        ent.Comp.LinkedServerId = null;
    }

    private void RefreshLinkedServer(Entity<QuantumConsoleComponent> ent)
    {
        if (!TryComp<DeviceLinkSinkComponent>(ent.Owner, out var sink))
            return;

        foreach (var source in sink.LinkedSources)
        {
            if (!HasComp<QuantumServerComponent>(source))
                continue;

            ent.Comp.LinkedServerId = source;
            return;
        }

        ent.Comp.LinkedServerId = null;
    }
}
