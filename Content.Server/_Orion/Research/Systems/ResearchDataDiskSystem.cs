using System.Linq;
using Content.Server._Orion.Research.Components;
using Content.Server.Popups;
using Content.Server.Research.Systems;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Research.Components;

namespace Content.Server._Orion.Research.Systems;

public sealed class ResearchDataDiskSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly ResearchSystem _research = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ResearchDataDiskComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<ResearchDataDiskComponent, ExaminedEvent>(OnExamined);
    }

    private void OnAfterInteract(EntityUid uid, ResearchDataDiskComponent component, AfterInteractEvent args)
    {
        if (!args.CanReach)
            return;

        if (!TryComp<ResearchServerComponent>(args.Target, out var server) ||
            !TryComp<TechnologyDatabaseComponent>(args.Target, out var database))
            return;

        if (component.HasDataSnapshot)
        {
            var imported = ImportDiskData(args.Target.Value, component, database);
            _popupSystem.PopupEntity(Loc.GetString("research-disk-data-imported", ("count", imported)), args.Target.Value, args.User);
            _research.LogNetworkEvent(args.Target.Value, "disk", Loc.GetString("research-netlog-disk-imported", ("count", imported)), args.User);
            args.Handled = true;
            return;
        }

        ExportDiskData(uid, component, database, server);
        _popupSystem.PopupEntity(Loc.GetString("research-disk-exported", ("count", component.StoredTechnologies.Count)), args.Target.Value, args.User);
        _research.LogNetworkEvent(args.Target.Value, "disk", Loc.GetString("research-netlog-disk-exported", ("count", component.StoredTechnologies.Count)), args.User);
        args.Handled = true;
    }

    private void ExportDiskData(EntityUid diskUid, ResearchDataDiskComponent disk, TechnologyDatabaseComponent database, ResearchServerComponent server)
    {
        disk.HasDataSnapshot = true;
        disk.StoredTechnologies = database.ResearchedTechnologies.Select(x => x.ToString()).ToList();
        disk.SnapshotServerName = server.ServerName;
        Dirty(diskUid, disk);
    }

    private int ImportDiskData(EntityUid serverUid, ResearchDataDiskComponent disk, TechnologyDatabaseComponent database)
    {
        return _research.ImportTechnologySnapshot(serverUid, disk.StoredTechnologies, database);
    }

    private void OnExamined(EntityUid uid, ResearchDataDiskComponent component, ExaminedEvent args)
    {
        if (!component.HasDataSnapshot)
        {
            args.PushMarkup(Loc.GetString("research-disk-data-empty"));
            return;
        }

        args.PushMarkup(Loc.GetString("research-disk-data-examine",
            ("server", string.IsNullOrWhiteSpace(component.SnapshotServerName) ? Loc.GetString("research-disk-data-unknown-server") : component.SnapshotServerName),
            ("count", component.StoredTechnologies.Count)));
    }
}
