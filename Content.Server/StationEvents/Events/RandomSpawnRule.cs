// SPDX-FileCopyrightText: 2024 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2024 deltanedas <39013340+deltanedas@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 deltanedas <@deltanedas:kde.org>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

public sealed class RandomSpawnRule : StationEventSystem<RandomSpawnRuleComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!; // Orion
    protected override void Started(EntityUid uid, RandomSpawnRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);

        // Orion-Edit-Start
        int spawnCount = _random.Next(comp.MinCount, comp.MaxCount + 1);

        for (int i = 0; i < spawnCount; i++)
        {
            if (TryFindRandomTile(out _, out _, out _, out var coords))
            {
                Sawmill.Info($"Spawning {comp.Prototype} at {coords}");
                Spawn(comp.Prototype, coords);
            }
        // Orion-Edit-End
        }
    }
}
