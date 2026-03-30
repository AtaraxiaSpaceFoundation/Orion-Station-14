// SPDX-FileCopyrightText: 2022 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: MIT

using Content.Shared._Orion.Research;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using Content.Shared.Research.Systems;

namespace Content.Client.Research;

public sealed class ResearchSystem : SharedResearchSystem
{
    // Orion-Start
    public List<ResearchPointAmount> GetTechnologyFinalPointCostsForUi(TechnologyDatabaseComponent database, TechnologyPrototype technology)
    {
        return GetTechnologyFinalPointCosts(database, technology);
    }
    // Orion-End
}
