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
                _research.LogNetworkEvent(args.Target.Value, "disk", $"Imported research disk data ({component.StoredTechnologies.Count} technologies).", args.User);
                args.Handled = true;
                return;
            }

            if (TryComp<TechnologyDatabaseComponent>(args.Target, out database) && component.Points <= 0)
            {
                ExportDiskData(uid, args.Target.Value, component, database, server);
                _popupSystem.PopupEntity(Loc.GetString("research-disk-inserted", ("points", component.StoredTechnologies.Count)), args.Target.Value, args.User);
                _research.LogNetworkEvent(args.Target.Value, "disk", $"Exported research disk data ({component.StoredTechnologies.Count} technologies).", args.User);
                args.Handled = true;
                return;
            }

            _research.ModifyServerPoints(args.Target.Value, component.Points, server);
            _research.LogNetworkEvent(args.Target.Value, "disk", $"Applied research points from disk: {component.Points}", args.User);
            _popupSystem.PopupEntity(Loc.GetString("research-disk-inserted", ("points", component.Points)), args.Target.Value, args.User);
            QueueDel(uid);
            args.Handled = true;
        }

        private void ExportDiskData(EntityUid diskUid, EntityUid serverUid, ResearchDiskComponent disk, TechnologyDatabaseComponent database, ResearchServerComponent server)
        {
            disk.StoredTechnologies = database.ResearchedTechnologies.Select(x => x.ToString()).ToList();
            disk.StoredPointBalances = server.PointBalances.ToList();
            Dirty(diskUid, disk);
        }

        private void ImportDiskData(EntityUid serverUid, ResearchDiskComponent disk, TechnologyDatabaseComponent database)
        {
            foreach (var tech in disk.StoredTechnologies)
            {
                if (!database.ResearchedTechnologies.Contains(tech))
                    database.ResearchedTechnologies.Add(tech);
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
