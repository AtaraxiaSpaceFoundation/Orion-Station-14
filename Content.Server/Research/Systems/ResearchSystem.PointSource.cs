// SPDX-FileCopyrightText: 2022 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: MIT

using Content.Server.Power.EntitySystems;
using Content.Server.Research.Components;
using Content.Shared._Orion.Research;
using Content.Shared._Orion.Research.Components;
using Content.Shared.Research.Components;

namespace Content.Server.Research.Systems;

public sealed partial class ResearchSystem
{
    private void InitializeSource()
    {
//        SubscribeLocalEvent<ResearchPointSourceComponent, ResearchServerGetPointsPerSecondEvent>(OnGetPointsPerSecond); // Orion-Edit
        SubscribeLocalEvent<ResearchPointSourceComponent, ResearchServerGetPointsPerSecondByTypeEvent>(OnGetPointsPerSecondByType); // Orion
    }

/* // Orion-Edit: Use OnGetPointsPerSecondByType
    private void OnGetPointsPerSecond(Entity<ResearchPointSourceComponent> source, ref ResearchServerGetPointsPerSecondEvent args)
    {
        // Orion-Start
        if (TryComp<ResearchServerControlStatusComponent>(args.Server, out var status) && !status.GenerationEnabled)
            return;
        // Orion-End

        if (CanProduce(source))
            args.Points += source.Comp.PointsPerSecond;
    }
*/

    private bool CanProduce(Entity<ResearchPointSourceComponent> source) // Orion-Edit: Was public
    {
        return source.Comp.Active && this.IsPowered(source, EntityManager);
    }

    // Orion-Start
    private void OnGetPointsPerSecondByType(Entity<ResearchPointSourceComponent> source, ref ResearchServerGetPointsPerSecondByTypeEvent args)
    {
        if (TryComp<ResearchServerControlStatusComponent>(args.Server, out var status) && !status.GenerationEnabled)
            return;

        if (!CanProduce(source))
            return;

        if (source.Comp.RequiredInfrastructure != null &&
            (!TryComp<TechnologyDatabaseComponent>(args.Server, out var db) ||
             !db.UnlockedInfrastructure.Contains(source.Comp.RequiredInfrastructure)))
        {
            return;
        }

        args.Points.Add(new ResearchPointAmount
        {
            Type = source.Comp.PointType,
            Amount = source.Comp.PointsPerSecond,
        });
    }
    // Orion-End
}
