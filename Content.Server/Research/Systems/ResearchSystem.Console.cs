// SPDX-FileCopyrightText: 2023 Chief-Engineer <119664036+Chief-Engineer@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Ed <96445749+TheShuEd@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Fildrance <fildrance@gmail.com>
// SPDX-FileCopyrightText: 2024 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 metalgearsloth <comedian_vs_clown@hotmail.com>
// SPDX-FileCopyrightText: 2024 pa.pecherskij <pa.pecherskij@interfax.ru>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <aviu00@protonmail.com>
// SPDX-FileCopyrightText: 2025 FaDeOkno <143940725+FaDeOkno@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 FaDeOkno <logkedr18@gmail.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 SX_7 <sn1.test.preria.2002@gmail.com>
// SPDX-FileCopyrightText: 2025 ScarKy0 <106310278+ScarKy0@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 amogus <113782077+whateverusername0@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 coderabbitai[bot] <136622811+coderabbitai[bot]@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
// SPDX-FileCopyrightText: 2025 whateverusername0 <whateveremail>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Common.Pirates;
using Content.Goobstation.Common.Research;
using Content.Server.Chat.Systems;
using Content.Server.Power.EntitySystems;
using Content.Server.Research.Components;
using Content.Shared._Orion.Research;
using Content.Shared._Orion.Research.Prototypes;
using Content.Shared.Access.Components;
using Content.Shared.Chat;
using Content.Shared.Emag.Systems;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using Content.Shared.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Server.Research.Systems;

public sealed partial class ResearchSystem
{
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly ChatSystem _chat = default!; // Orion

    private void InitializeConsole()
    {
        SubscribeLocalEvent<ResearchConsoleComponent, ConsoleUnlockTechnologyMessage>(OnConsoleUnlock);
        SubscribeLocalEvent<ResearchConsoleComponent, BeforeActivatableUIOpenEvent>(OnConsoleBeforeUiOpened);
        SubscribeLocalEvent<ResearchConsoleComponent, ResearchServerPointsChangedEvent>(OnPointsChanged);
        SubscribeLocalEvent<ResearchConsoleComponent, ResearchRegistrationChangedEvent>(OnConsoleRegistrationChanged);
        SubscribeLocalEvent<ResearchConsoleComponent, TechnologyDatabaseModifiedEvent>(OnConsoleDatabaseModified);
        SubscribeLocalEvent<ResearchConsoleComponent, TechnologyDatabaseSynchronizedEvent>(OnConsoleDatabaseSynchronized);
//        SubscribeLocalEvent<ResearchConsoleComponent, GotEmaggedEvent>(OnEmagged); // Orion-Edit
    }

    private void OnConsoleUnlock(EntityUid uid, ResearchConsoleComponent component, ConsoleUnlockTechnologyMessage args)
    {
        // goob edit - spirates
        var eqe = EntityQueryEnumerator<ResourceSiphonComponent>();
        while (eqe.MoveNext(out var siphon))
        {
            if (siphon.Active)
            {
                _popup.PopupEntity(Loc.GetString("console-block-something"), args.Actor);
                return;
            }
        }
        // goob edit end

        var act = args.Actor;

        if (!this.IsPowered(uid, EntityManager))
            return;

        if (!PrototypeManager.TryIndex<TechnologyPrototype>(args.Id, out var technologyPrototype))
            return;

        if (TryComp<AccessReaderComponent>(uid, out var access) && !_accessReader.IsAllowed(act, uid, access))
        {
            _popup.PopupEntity(Loc.GetString("research-console-no-access-popup"), act);
            return;
        }

        if (!UnlockTechnology(uid, args.Id, act))
        {
            // Orion-Start
            _popup.PopupEntity(Loc.GetString("research-console-unlock-failed-popup"), act);
            // Orion-End
            return;
        }

        if (!_emag.CheckFlag(uid, EmagType.Interaction) && technologyPrototype.AnnounceOnUnlock) // Orion-Edit
        {
            var costText = FormatResearchPointAmounts(technologyPrototype.PointCosts); // Orion
            var message = Loc.GetString(
                "research-console-unlock-technology-radio-broadcast",
                ("technology", Loc.GetString(technologyPrototype.Name)),
                ("amount", costText)); // Orion-Edit: Removed approver

            // Orion-Start
            var messageIC = Loc.GetString(
                "research-console-unlock-technology-ic",
                ("technology", Loc.GetString(technologyPrototype.Name)),
                ("amount", costText));
            // Orion-End

            // Orion-Edit-Start: More than one channel announce
            var announceChannels = technologyPrototype.AnnounceChannels.Count > 0
                ? technologyPrototype.AnnounceChannels
                : [component.AnnouncementChannel];

            foreach (var channel in announceChannels)
            {
                _radio.SendRadioMessage(uid, message, channel, uid, escapeMarkup: false);
            }
            // Orion-Edit-End

            _chat.TrySendInGameICMessage(uid, messageIC, InGameICChatType.Speak, false); // Orion
        }

        SyncClientWithServer(uid);
        UpdateConsoleInterface(uid, component);
    }

    private void OnConsoleBeforeUiOpened(EntityUid uid, ResearchConsoleComponent component, BeforeActivatableUIOpenEvent args)
    {
        SyncClientWithServer(uid);
    }

    private void UpdateConsoleInterface(EntityUid uid, ResearchConsoleComponent? component = null, ResearchClientComponent? clientComponent = null)
    {
        if (!Resolve(uid, ref component, ref clientComponent, false))
            return;

        // R&D Console Rework Start
        Dictionary<string, ResearchAvailability> techList;
        // Orion-Start
        Dictionary<string, ResearchTechnologyLockReason> lockReasons;
        List<ResearchConsoleExperimentData> experiments;
        var networkId = string.Empty;
        List<ResearchPointAmount> pointBalances = new();
        List<ResearchLogEntry> logs = new();
        TechnologyDatabaseComponent? syncedDb = null;
        // Orion-End
        var points = 0;

        if (TryGetClientServer(uid, out var serverUid, out var server, clientComponent) &&
            TryComp<TechnologyDatabaseComponent>(serverUid, out var db))
        {
/* // Orion-Edit
            var unlockedTechs = new HashSet<ProtoId<TechnologyPrototype>>(db.UnlockedTechnologies);
            techList = allTechs.ToDictionary(
                proto => proto.ID,
                proto =>
*/
            // Orion-Start
            syncedDb = db;
            networkId = server.NetworkId;
            pointBalances = server.PointBalances.ToList();
            logs = server.Logs.ToList();
            var visible = new HashSet<ProtoId<TechnologyPrototype>>(db.VisibleTechnologies);
            var available = new HashSet<ProtoId<TechnologyPrototype>>(db.AvailableTechnologies);
            var researched = new HashSet<ProtoId<TechnologyPrototype>>(db.ResearchedTechnologies);

            techList = visible.ToDictionary(
                techId => techId.ToString(),
                techId =>
            // Orion-End
                {
                    if (researched.Contains(techId)) // Orion-Edit
                        return ResearchAvailability.Researched;

/* // Orion-Edit
                    var prereqsMet = proto.TechnologyPrerequisites.All(p => unlockedTechs.Contains(p));
                    var canAfford = server.Points >= proto.Cost;
*/

                    // Orion-Start
                    if (!available.Contains(techId))
                        return ResearchAvailability.PrereqsMet;

                    var proto = PrototypeManager.Index(techId);
                    var allCosts = GetTechnologyFinalPointCosts(db, proto);
                    var canAfford = HasSufficientPoints(serverUid.Value, allCosts, server);
                    // Orion-End

                    return canAfford // Orion-Edit
                        ? ResearchAvailability.Available // Orion-Edit
                        : ResearchAvailability.Unavailable;
                });

            // Orion-Start
            lockReasons = PrototypeManager.EnumeratePrototypes<TechnologyPrototype>()
                .Where(proto => db.SupportedDisciplines.Contains(proto.Discipline))
                .ToDictionary(proto => proto.ID,
                    proto =>
                {
                    var reason = GetTechnologyLockReason(db, proto);
                    if (reason != ResearchTechnologyLockReason.None)
                        return reason;

                    var costs = GetTechnologyFinalPointCosts(db, proto);

                    if (!HasSufficientPoints(serverUid.Value, costs, server) && !db.ResearchedTechnologies.Contains(proto.ID))
                        return ResearchTechnologyLockReason.InsufficientPoints;

                    return reason;
                });

            experiments = BuildExperimentUiData(db);
            // Orion-End

            if (clientComponent != null)
                points = clientComponent.ConnectedToServer ? server.Points : 0;
        }
        else
        {
            techList = new Dictionary<string, ResearchAvailability>(); // Orion-Edit
            // Orion-Start
            lockReasons = new Dictionary<string, ResearchTechnologyLockReason>();
            experiments = new List<ResearchConsoleExperimentData>();
            // Orion-End
        }

        // Orion-Edit-Start
        _uiSystem.SetUiState(uid,
            ResearchConsoleUiKey.Key,
            new ResearchConsoleBoundInterfaceState(
                points,
                techList,
                syncedDb?.VisibleTechnologies.ToList() ?? new List<ProtoId<TechnologyPrototype>>(),
                syncedDb?.AvailableTechnologies.ToList() ?? new List<ProtoId<TechnologyPrototype>>(),
                syncedDb?.ResearchedTechnologies.ToList() ?? new List<ProtoId<TechnologyPrototype>>(),
                syncedDb?.CompletedExperiments.ToList() ?? new List<string>(),
                experiments,
                lockReasons,
                networkId,
                pointBalances,
                logs));
        // Orion-Edit-End
        // R&D Console Rework End
    }

    // Orion-Start
    private List<ResearchConsoleExperimentData> BuildExperimentUiData(TechnologyDatabaseComponent database)
    {
        var data = new List<ResearchConsoleExperimentData>();
        foreach (var experiment in PrototypeManager.EnumeratePrototypes<ResearchExperimentPrototype>())
        {
            if (experiment.Hidden)
                continue;

            var progress = database.ExperimentProgress.FirstOrDefault(p => p.ExperimentId == experiment.ID);
            var state = ResearchExperimentState.Unavailable;

            if (database.SkippedExperiments.Contains(experiment.ID))
                state = ResearchExperimentState.Skipped;
            else if (database.CompletedExperiments.Contains(experiment.ID))
                state = ResearchExperimentState.Completed;
            else if (database.ActiveExperiments.Contains(experiment.ID))
                state = ResearchExperimentState.Active;
            else if (database.AvailableExperiments.Contains(experiment.ID))
                state = ResearchExperimentState.Available;

            if (state == ResearchExperimentState.Unavailable)
                continue;

            var target = progress.Target > 0 ? progress.Target : Math.Max(1, experiment.Objective.Target);
            data.Add(new ResearchConsoleExperimentData(experiment.ID, progress.Progress, target, state));
        }

        return data.OrderByDescending(e => e.State == ResearchExperimentState.Active)
            .ThenBy(e => e.Id)
            .ToList();
    }
    // Orion-End

    private void OnPointsChanged(EntityUid uid, ResearchConsoleComponent component, ref ResearchServerPointsChangedEvent args)
    {
        if (!_uiSystem.IsUiOpen(uid, ResearchConsoleUiKey.Key))
            return;
        UpdateConsoleInterface(uid, component);
    }

    private void OnConsoleRegistrationChanged(EntityUid uid, ResearchConsoleComponent component, ref ResearchRegistrationChangedEvent args)
    {
        SyncClientWithServer(uid);
        UpdateConsoleInterface(uid, component);
    }

    private void OnConsoleDatabaseModified(EntityUid uid, ResearchConsoleComponent component, ref TechnologyDatabaseModifiedEvent args)
    {
        SyncClientWithServer(uid);
        UpdateConsoleInterface(uid, component);
    }

    private void OnConsoleDatabaseSynchronized(EntityUid uid, ResearchConsoleComponent component, ref TechnologyDatabaseSynchronizedEvent args)
    {
        UpdateConsoleInterface(uid, component);
    }

/* // Orion-Edit
    private void OnEmagged(Entity<ResearchConsoleComponent> ent, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(ent, EmagType.Interaction))
            return;

        args.Handled = true;
    }
*/

    // Orion-Start
    private static string BuildCorruptedMessage(string message)
    {
        const string symbols = "!@#$%^&*?№";
        var chars = message.ToCharArray();
        var replaced = 0;

        for (var i = 0; i < chars.Length; i++)
        {
            if (!char.IsLetter(chars[i]) || Random.Shared.NextDouble() > 0.24)
                continue;

            chars[i] = symbols[Random.Shared.Next(symbols.Length)];
            replaced++;
        }

        if (replaced != 0)
            return new string(chars);
        {
            for (var i = 0; i < chars.Length; i++)
            {
                if (!char.IsLetter(chars[i]))
                    continue;

                chars[i] = symbols[Random.Shared.Next(symbols.Length)];
                break;
            }
        }

        return new string(chars);
    }
    // Orion-End
}

public sealed class ResearchConsoleUnlockEvent : CancellableEntityEventArgs { }
