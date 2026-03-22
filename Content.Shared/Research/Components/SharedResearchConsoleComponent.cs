// SPDX-FileCopyrightText: 2019 Pieter-Jan Briers <pieterjan.briers@gmail.com>
// SPDX-FileCopyrightText: 2019 Víctor Aguilera Puerto <6766154+Zumorica@users.noreply.github.com>
// SPDX-FileCopyrightText: 2021 Acruid <shatter66@gmail.com>
// SPDX-FileCopyrightText: 2021 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2021 Metal Gear Sloth <metalgearsloth@gmail.com>
// SPDX-FileCopyrightText: 2021 Vera Aguilera Puerto <6766154+Zumorica@users.noreply.github.com>
// SPDX-FileCopyrightText: 2021 Visne <39844191+Visne@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 mirrorcult <lunarautomaton6@gmail.com>
// SPDX-FileCopyrightText: 2022 wrexbe <81056464+wrexbe@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 FaDeOkno <143940725+FaDeOkno@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 FaDeOkno <logkedr18@gmail.com>
// SPDX-FileCopyrightText: 2025 coderabbitai[bot] <136622811+coderabbitai[bot]@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Research;
using Content.Shared._Orion.Research;
using Content.Shared.Research.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Research.Components
{
    [NetSerializable, Serializable]
    public enum ResearchConsoleUiKey : byte
    {
        Key,
    }

    [Serializable, NetSerializable]
    public sealed class ConsoleUnlockTechnologyMessage : BoundUserInterfaceMessage
    {
        public string Id;

        public ConsoleUnlockTechnologyMessage(string id)
        {
            Id = id;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ConsoleServerSelectionMessage : BoundUserInterfaceMessage
    {

    }

    [Serializable, NetSerializable]
    public sealed class ResearchConsoleBoundInterfaceState : BoundUserInterfaceState
    {
        public int Points;

        /// <summary>
        /// Goobstation field - all researches and their availablities
        /// </summary>
        public Dictionary<string, ResearchAvailability> Researches;
        // Orion-Start
        public List<ProtoId<TechnologyPrototype>> VisibleTechnologies;
        public List<ProtoId<TechnologyPrototype>> AvailableTechnologies;
        public List<ProtoId<TechnologyPrototype>> ResearchedTechnologies;
        public List<string> CompletedExperiments;
        public List<ResearchConsoleExperimentData> Experiments;
        public Dictionary<string, ResearchTechnologyLockReason> TechnologyLockReasons;
        public string NetworkId;
        public List<ResearchPointAmount> PointBalances;
        public List<ResearchLogEntry> Logs;
        // Orion-End

        // Orion-Edit-Start
        public ResearchConsoleBoundInterfaceState(
            int points,
            Dictionary<string, ResearchAvailability> researches,
            List<ProtoId<TechnologyPrototype>> visibleTechnologies,
            List<ProtoId<TechnologyPrototype>> availableTechnologies,
            List<ProtoId<TechnologyPrototype>> researchedTechnologies,
            List<string> completedExperiments,
            List<ResearchConsoleExperimentData> experiments,
            Dictionary<string, ResearchTechnologyLockReason> technologyLockReasons,
            string networkId,
            List<ResearchPointAmount> pointBalances,
            List<ResearchLogEntry> logs) // Goobstation R&D console rework = researches field
        // Orion-Edit-End
        {
            Points = points;
            Researches = researches;    // Goobstation R&D console rework
            // Orion-Start
            VisibleTechnologies = visibleTechnologies;
            AvailableTechnologies = availableTechnologies;
            ResearchedTechnologies = researchedTechnologies;
            CompletedExperiments = completedExperiments;
            Experiments = experiments;
            TechnologyLockReasons = technologyLockReasons;
            NetworkId = networkId;
            PointBalances = pointBalances;
            Logs = logs;
            // Orion-End
        }
    }

    // Orion-Start
    [Serializable, NetSerializable]
    public sealed class ResearchConsoleExperimentData
    {
        public string Id;
        public int Progress;
        public int Target;
        public ResearchExperimentState State;

        public ResearchConsoleExperimentData(string id, int progress, int target, ResearchExperimentState state)
        {
            Id = id;
            Progress = progress;
            Target = target;
            State = state;
        }
    }
    // Orion-End
}
