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
using Content.Shared._Orion.Research;
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

            // Orion-Edit-Start
            if (component.PointBalances.Count > 0)
            {
                foreach (var balance in component.PointBalances)
                {
                    _research.ModifyServerPoints(args.Target.Value, balance.Type, balance.Amount, server);
                }
            }
            else
            {
                _research.ModifyServerPoints(args.Target.Value, component.Points, server);
            }
            // Orion-Edit-End

            _research.LogNetworkEvent(args.Target.Value, "disk", Loc.GetString("research-netlog-disk-points-applied", ("points", component.Points)), args.User); // Orion
            _popupSystem.PopupEntity(Loc.GetString("research-disk-inserted", ("points", component.Points)), args.Target.Value, args.User);
            QueueDel(uid);
            args.Handled = true;
        }

        private void OnMapInit(EntityUid uid, ResearchDiskComponent component, MapInitEvent args)
        {
            if (!component.UnlockAllTech)
                return;

            component.Points = _prototype.EnumeratePrototypes<TechnologyPrototype>()
                // Orion-Edit-Start
                .Sum(tech => tech.PointCosts
                    .Where(cost => cost.Type == "General")
                    .Sum(cost => cost.Amount));
                // Orion-Edit-End

                // Orion-Start
                component.PointBalances = _prototype.EnumeratePrototypes<TechnologyPrototype>()
                    .SelectMany(tech => tech.PointCosts)
                    .GroupBy(cost => cost.Type)
                    .Select(group => new ResearchPointAmount
                    {
                        Type = group.Key,
                        Amount = group.Sum(cost => cost.Amount),
                    })
                    .ToList();
                // Orion-End
        }
    }
}
