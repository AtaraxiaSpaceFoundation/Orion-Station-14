// SPDX-FileCopyrightText: 2022 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 Rane <60792108+Elijahrane@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 metalgearsloth <comedian_vs_clown@hotmail.com>
// SPDX-FileCopyrightText: 2023 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 SolStar <44028047+ewokswagger@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Popups;
using Content.Server.Research.Systems;
using Content.Shared.Interaction;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Research.Disk
{
    public sealed class ResearchDiskSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototype = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly ResearchSystem _research = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ResearchDiskComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<ResearchDiskComponent, MapInitEvent>(OnMapInit);
        }

        private void OnAfterInteract(EntityUid uid, ResearchDiskComponent component, AfterInteractEvent args)
        {
            if (!args.CanReach)
                return;

            if (!TryComp<ResearchServerComponent>(args.Target, out var server))
                return;

            if (TryComp<TechnologyDatabaseComponent>(args.Target, out var database) &&
                (component.StoredTechnologies.Count > 0 || component.StoredPointBalances.Count > 0))
            {
                ImportDiskData(args.Target.Value, component, database);
                _popupSystem.PopupEntity(Loc.GetString("research-disk-inserted", ("points", component.Points)), args.Target.Value, args.User);
                _research.LogNetworkEvent(args.Target.Value, "disk", Loc.GetString("research-netlog-disk-imported", ("count", component.StoredTechnologies.Count)), args.User);
                args.Handled = true;
                return;
            }

            if (database != null && component.Points <= 0)
            {
                ExportDiskData(uid, component, database, server);
                _popupSystem.PopupEntity(Loc.GetString("research-disk-exported", ("count", component.StoredTechnologies.Count)), args.Target.Value, args.User);
                _research.LogNetworkEvent(args.Target.Value, "disk", Loc.GetString("research-netlog-disk-exported", ("count", component.StoredTechnologies.Count)), args.User);
                args.Handled = true;
                return;
            }

            _research.ModifyServerPoints(args.Target.Value, component.Points, server);
            _research.LogNetworkEvent(args.Target.Value, "disk", Loc.GetString("research-netlog-disk-points-applied", ("points", component.Points)), args.User);
            _popupSystem.PopupEntity(Loc.GetString("research-disk-inserted", ("points", component.Points)), args.Target.Value, args.User);
            QueueDel(uid);
            args.Handled = true;
        }

        private void ExportDiskData(EntityUid diskUid, ResearchDiskComponent disk, TechnologyDatabaseComponent database, ResearchServerComponent server)
        {
            disk.StoredTechnologies = database.ResearchedTechnologies.Select(x => x.ToString()).ToList();
            disk.StoredPointBalances = server.PointBalances.ToList();
            Dirty(diskUid, disk);
        }

        private void ImportDiskData(EntityUid serverUid, ResearchDiskComponent disk, TechnologyDatabaseComponent database)
        {
            foreach (var tech in disk.StoredTechnologies)
            {
                _research.AddTechnology(serverUid, tech, database);
            }

            if (TryComp<ResearchServerComponent>(serverUid, out var server))
            {
                foreach (var point in disk.StoredPointBalances)
                {
                    _research.ModifyServerPoints(serverUid, point.Type, point.Amount, server);
                }
            }

            _research.RecalculateTechnologyState(serverUid, database);
            _research.UpdateTechnologyCards(serverUid, database);
            Dirty(serverUid, database);
        }

        private void OnMapInit(EntityUid uid, ResearchDiskComponent component, MapInitEvent args)
        {
            if (!component.UnlockAllTech)
                return;

            component.Points = _prototype.EnumeratePrototypes<TechnologyPrototype>()
                .Sum(tech => tech.Cost);
        }
    }
}
