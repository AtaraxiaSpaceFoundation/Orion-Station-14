using Content.Server._Orion.Arrivals.CentComm.Events;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.Popups;
using Content.Server.Power.EntitySystems;
using Content.Server.Salvage.Expeditions;
using Content.Server.Shuttles;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared._Orion.Abilities;
using Content.Shared._Orion.Arrivals.Components;
using Content.Shared.Cargo.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Shuttles.Components;
using Content.Shared.Whitelist;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._Orion.Arrivals.CentComm;

public sealed class CentCommSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;
    [Dependency] private readonly ShuttleConsoleSystem _console = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    private ISawmill _log = default!;
    public EntityUid CentCommGrid { get; private set; } = EntityUid.Invalid;
    public MapId CentCommMap { get; private set; } = MapId.Nullspace;
    public Entity<MapComponent>? CentCommMapUid { get; private set; }
    public float ShuttleIndex { get; set; }

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ActorComponent, CentCommFtlAction>(OnFtlActionUsed);
        SubscribeLocalEvent<RoundEndedEvent>(OnCentComEndRound);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnCleanup);
        SubscribeLocalEvent<ShuttleConsoleComponent, GotEmaggedEvent>(OnShuttleConsoleEmaged);
        SubscribeLocalEvent<FTLCompletedEvent>(OnFTLCompleted);
        SubscribeLocalEvent<FTLCentCommEvent>(OnFtlAnnounce);

        _log = Logger.GetSawmill("centcomm.map");
    }

    private void OnCentComEndRound(RoundEndedEvent ev)
    {
        if (CentCommMapUid != null && _shuttle.TryAddFTLDestination(CentCommMap, true, out var ftl))
            EnableFtl((CentCommMapUid.Value, ftl));
    }

    private readonly HashSet<Entity<IFFConsoleComponent>> _iFfConsoleEntities = new();
    private void OnFtlAnnounce(FTLCentCommEvent ev)
    {
        if (!CentCommGrid.IsValid())
            return; // Not loaded CentComm

        var shuttleName = "Unknown";

        _iFfConsoleEntities.Clear();
        _lookup.GetGridEntities(ev.Source, _iFfConsoleEntities);

        foreach (var (_, iff) in _iFfConsoleEntities)
        {
            var f = iff.AllowedFlags;
            if (f.HasFlag(IFFFlags.Hide))
                continue;

            var name = MetaData(ev.Source).EntityName;
            if (string.IsNullOrWhiteSpace(name))
                continue;

            shuttleName = name;
        }

        if (_gameTicker.RunLevel != GameRunLevel.InRound)
            return; // Do not announce out of round

        _chat.DispatchStationAnnouncement(CentCommGrid,
            $"Внимание! Радары обнаружили {shuttleName} шаттл, входящий в космическое пространство объекта Центрального Командования!",
            "Радар",
            colorOverride: Color.Crimson);
    }

    private void OnFTLCompleted(ref FTLCompletedEvent ev)
    {
        if (!CentCommGrid.IsValid())
            return; // Not loaded CentComm

        var xform = Transform(ev.Entity);
        if (xform.MapID != CentCommMap)
            return; // Not CentComm

        if (!TryComp<ShuttleComponent>(ev.Entity, out var shuttleComponent))
            return;

        QueueLocalEvent(new FTLCentCommEvent
        {
            Source = (ev.Entity, shuttleComponent),
        });
    }

    private static readonly SoundSpecifier SparkSound = new SoundCollectionSpecifier("sparks");

    [ValidatePrototypeId<EntityPrototype>]
    private const string StationShuttleConsole = "ComputerShuttle";

    private void OnShuttleConsoleEmaged(Entity<ShuttleConsoleComponent> ent, ref GotEmaggedEvent args)
    {
        if (Prototype(ent)?.ID != StationShuttleConsole)
            return;

        if (!this.IsPowered(ent, EntityManager))
            return;

        var shuttle = Transform(ent).GridUid;
        if (!HasComp<ShuttleComponent>(shuttle))
            return;

        if (!(HasComp<CargoShuttleComponent>(shuttle) || HasComp<SalvageShuttleComponent>(shuttle)))
            return;

        _audio.PlayPvs(SparkSound, ent);
        _popup.PopupEntity(Loc.GetString("shuttle-console-component-upgrade-emag-requirement"), ent);
        args.Handled = true;
        EnsureComp<AllowCentCommFTLComponent>(shuttle.Value); // для обновления консоли нужно чтобы компонент был до вызыва RefreshShuttleConsoles
        _console.RefreshShuttleConsoles();
    }

    private void OnCleanup(RoundRestartCleanupEvent ev)
    {
        _log.Info("OnCleanup");
        QueueDel(CentCommGrid);
        CentCommGrid = EntityUid.Invalid;

        if (_map.MapExists(CentCommMap))
            _map.DeleteMap(CentCommMap);

        CentCommMap = MapId.Nullspace;
        CentCommMapUid = null;
        ShuttleIndex = 0;
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public void DisableFtl(Entity<FTLDestinationComponent?> ent)
    {
        if(!Resolve(ent, ref ent.Comp))
            return;

        var d = new EntityWhitelist
        {
            RequireAll = false,
            Components = ["AllowCentCommFTLComponent"],
        };

        ent.Comp.RequireCoordinateDisk = false;
        ent.Comp.BeaconsOnly = false;

        _shuttle.SetFTLWhitelist(ent, d);
    }

    public void EnableFtl(Entity<FTLDestinationComponent?> ent)
    {
        if(!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.RequireCoordinateDisk = false;
        ent.Comp.BeaconsOnly = false;

        _shuttle.SetFTLWhitelist(ent, null);
    }

    private void OnFtlActionUsed(EntityUid uid, ActorComponent component, CentCommFtlAction args)
    {
        var grid = Transform(args.Performer);
        if (grid.GridUid == null)
            return;

        if (!TryComp<PilotComponent>(args.Performer, out var pilotComponent) || pilotComponent.Console == null)
        {
            _popup.PopupEntity(Loc.GetString("centcomm-ftl-action-no-pilot"), args.Performer, args.Performer);
            return;
        }

        TransformComponent shuttle;

        if (TryComp<DroneConsoleComponent>(pilotComponent.Console, out var droneConsoleComponent) &&
            droneConsoleComponent.Entity != null)
        {
            shuttle = Transform(droneConsoleComponent.Entity.Value);
        }
        else
        {
            shuttle = grid;
        }


        if (!TryComp<ShuttleComponent>(shuttle.GridUid, out var comp) || HasComp<FTLComponent>(shuttle.GridUid) || (
                HasComp<BecomesStationComponent>(shuttle.GridUid) &&
                !(
                    HasComp<SalvageShuttleComponent>(shuttle.GridUid) ||
                    HasComp<CargoShuttleComponent>(shuttle.GridUid)
                )
            ))
        {
            return;
        }

        var stationUid = _stationSystem.GetStations().FirstOrNull(HasComp<StationCentcommComponent>);

        if (!TryComp<StationCentcommComponent>(stationUid, out var centcomm) ||
             centcomm.Entity == null || !centcomm.Entity.Value.IsValid() || Deleted(centcomm.Entity))
        {
            _popup.PopupEntity(Loc.GetString("centcomm-ftl-action-no-station"), args.Performer, args.Performer);
            return;
        }
/*
        if (shuttle.MapUid == centcomm.MapEntity)
        {
            _popup.PopupEntity(Loc.GetString("centcom-ftl-action-at-centcomm"), args.Performer, args.Performer);
            return;
        }
*/
        if (!_shuttle.CanFTL(shuttle.GridUid.Value, out var reason))
        {
            _popup.PopupEntity(reason, args.Performer, args.Performer);
            return;
        }

        _shuttle.FTLToDock(shuttle.GridUid.Value, comp, centcomm.Entity.Value);
    }
}
