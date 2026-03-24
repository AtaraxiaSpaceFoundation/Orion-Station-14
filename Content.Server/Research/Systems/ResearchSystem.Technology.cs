// SPDX-FileCopyrightText: 2022 Paul Ritter <ritter.paul1@googlemail.com>
// SPDX-FileCopyrightText: 2024 Alice "Arimah" Heurlin <30327355+arimah@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 BombasterDS <115770678+BombasterDS@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Chief-Engineer <119664036+Chief-Engineer@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 DrSmugleaf <10968691+DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Ed <96445749+TheShuEd@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Errant <35878406+Errant-4@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Flareguy <78941145+Flareguy@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 HS <81934438+HolySSSS@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 IProduceWidgets <107586145+IProduceWidgets@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Mr. 27 <45323883+Dutch-VanDerLinde@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 PJBot <pieterjan.briers+bot@gmail.com>
// SPDX-FileCopyrightText: 2024 Pieter-Jan Briers <pieterjan.briers+git@gmail.com>
// SPDX-FileCopyrightText: 2024 Plykiya <58439124+Plykiya@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Rouge2t7 <81053047+Sarahon@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Tayrtahn <tayrtahn@gmail.com>
// SPDX-FileCopyrightText: 2024 Truoizys <153248924+Truoizys@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 TsjipTsjip <19798667+TsjipTsjip@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Ubaser <134914314+UbaserB@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Vasilis <vasilis@pikachu.systems>
// SPDX-FileCopyrightText: 2024 beck-thompson <107373427+beck-thompson@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 deltanedas <39013340+deltanedas@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 deltanedas <@deltanedas:kde.org>
// SPDX-FileCopyrightText: 2024 lzk <124214523+lzk228@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 osjarw <62134478+osjarw@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 plykiya <plykiya@protonmail.com>
// SPDX-FileCopyrightText: 2024 Арт <123451459+JustArt1m@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 FaDeOkno <143940725+FaDeOkno@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 FaDeOkno <logkedr18@gmail.com>
// SPDX-FileCopyrightText: 2025 SX_7 <sn1.test.preria.2002@gmail.com>
// SPDX-FileCopyrightText: 2025 coderabbitai[bot] <136622811+coderabbitai[bot]@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared._Orion.Research;
using Content.Shared.Database;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Research.Systems;

public sealed partial class ResearchSystem
{
    private readonly ISawmill _sawmill = Logger.GetSawmill("research.techweb"); // Orion

    /// <summary>
    /// Syncs the primary entity's database to that of the secondary entity's database.
    /// </summary>
    private void Sync(EntityUid primaryUid, EntityUid otherUid, TechnologyDatabaseComponent? primaryDb = null, TechnologyDatabaseComponent? otherDb = null) // Orion-Edit: Was public
    {
        if (!Resolve(primaryUid, ref primaryDb) || !Resolve(otherUid, ref otherDb))
            return;

        primaryDb.MainDiscipline = otherDb.MainDiscipline;
        primaryDb.CurrentTechnologyCards = otherDb.CurrentTechnologyCards;
        primaryDb.SupportedDisciplines = otherDb.SupportedDisciplines;
        // Orion-Start
        primaryDb.VisibleTechnologies = new List<ProtoId<TechnologyPrototype>>(otherDb.VisibleTechnologies);
        primaryDb.AvailableTechnologies = new List<ProtoId<TechnologyPrototype>>(otherDb.AvailableTechnologies);
        primaryDb.ResearchedTechnologies = new List<ProtoId<TechnologyPrototype>>(otherDb.ResearchedTechnologies);
        primaryDb.AvailableExperiments = otherDb.AvailableExperiments;
        primaryDb.UnlockedExperiments = otherDb.UnlockedExperiments;
        primaryDb.ActiveExperiments = otherDb.ActiveExperiments;
        primaryDb.CompletedExperiments = otherDb.CompletedExperiments;
        primaryDb.SkippedExperiments = otherDb.SkippedExperiments;
        primaryDb.ExperimentProgress = otherDb.ExperimentProgress;
        // Orion-End
        primaryDb.UnlockedRecipes = otherDb.UnlockedRecipes;
        // Orion-Start
        primaryDb.RevealedTechnologies = otherDb.RevealedTechnologies;
        primaryDb.DiscoveryProgress = otherDb.DiscoveryProgress;
        primaryDb.UnlockedInfrastructure = otherDb.UnlockedInfrastructure;
        // Orion-End

        Dirty(primaryUid, primaryDb);

        var ev = new TechnologyDatabaseSynchronizedEvent();
        RaiseLocalEvent(primaryUid, ref ev);
    }

    /// <summary>
    ///     If there's a research client component attached to the owner entity,
    ///     and the research client is connected to a research server, this method
    ///     syncs against the research server, and the server against the local database.
    /// </summary>
    /// <returns>Whether it could sync or not</returns>
    private void SyncClientWithServer(EntityUid uid, TechnologyDatabaseComponent? databaseComponent = null, ResearchClientComponent? clientComponent = null) // Orion-Edit: Was public
    {
        if (!Resolve(uid, ref databaseComponent, ref clientComponent, false))
            return;

        if (!TryComp<TechnologyDatabaseComponent>(clientComponent.Server, out var serverDatabase))
            return;

        Sync(uid, clientComponent.Server.Value, databaseComponent, serverDatabase);
    }

    /// <summary>
    /// Tries to add a technology to a database, checking if it is able to
    /// </summary>
    /// <returns>If the technology was successfully added</returns>
    private bool UnlockTechnology(EntityUid client, // Orion-Edit: Was public
        string prototypeid,
        EntityUid user,
        ResearchClientComponent? component = null,
        TechnologyDatabaseComponent? clientDatabase = null)
    {
        if (!PrototypeManager.TryIndex<TechnologyPrototype>(prototypeid, out var prototype))
            return false;

        return UnlockTechnology(client, prototype, user, component, clientDatabase);
    }

    /// <summary>
    /// Tries to add a technology to a database, checking if it is able to
    /// </summary>
    /// <returns>If the technology was successfully added</returns>
    private bool UnlockTechnology(EntityUid client, // Orion-Edit: Was public
        TechnologyPrototype prototype,
        EntityUid user,
        ResearchClientComponent? component = null,
        TechnologyDatabaseComponent? clientDatabase = null)
    {
        if (!Resolve(client, ref component, ref clientDatabase, false))
            return false;

        if (!TryGetClientServer(client, out var serverEnt, out _, component))
            return false;

        if (!CanServerUnlockTechnology(client, prototype, out var finalCosts, clientDatabase, component)) // Orion-Edit
            return false;

        AddTechnology(serverEnt.Value, prototype);
        //TrySetMainDiscipline(prototype, serverEnt.Value); // Goobstation commented
        TryConsumePoints(serverEnt.Value, finalCosts); // Orion-Edit
        UpdateTechnologyCards(serverEnt.Value);

        _adminLog.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(user):player} unlocked {prototype.ID} (discipline: {prototype.Discipline}, tier: {prototype.Tier}) at {ToPrettyString(client)}, for server {ToPrettyString(serverEnt.Value)}.");
        LogNetworkEvent(serverEnt.Value, "technology", Loc.GetString("research-netlog-technology-unlocked", ("technology", Loc.GetString(prototype.Name)), ("user", GetResearchLogUserName(user))), user); // Orion
        return true;
    }

    /// <summary>
    ///     Adds a technology to the database without checking if it could be unlocked.
    /// </summary>
    [PublicAPI]
    public void AddTechnology(EntityUid uid, string technology, TechnologyDatabaseComponent? component = null) // Orion-Edit: Was public
    {
        if (!Resolve(uid, ref component))
            return;

        if (!PrototypeManager.TryIndex<TechnologyPrototype>(technology, out var prototype))
            return;
        AddTechnology(uid, prototype, component);
    }

    /// <summary>
    ///     Adds a technology to the database without checking if it could be unlocked.
    /// </summary>
    private void AddTechnology(EntityUid uid, TechnologyPrototype technology, TechnologyDatabaseComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        //todo this needs to support some other stuff, too
        foreach (var generic in technology.GenericUnlocks)
        {
            if (generic.PurchaseEvent != null)
                RaiseLocalEvent(generic.PurchaseEvent);
        }

        // Orion-Edit-Start
        if (!component.ResearchedTechnologies.Contains(technology.ID))
            component.ResearchedTechnologies.Add(technology.ID);

        foreach (var experiment in technology.UnlockedExperiments)
        {
            if (component.CompletedExperiments.Contains(experiment) || component.SkippedExperiments.Contains(experiment))
                continue;

            if (!component.AvailableExperiments.Contains(experiment))
                component.AvailableExperiments.Add(experiment);
        }
        // Orion-Edit-End

        // Orion-Start
        foreach (var infrastructure in technology.InfrastructureUnlocks)
        {
            if (!component.UnlockedInfrastructure.Contains(infrastructure))
                component.UnlockedInfrastructure.Add(infrastructure);
        }

        if (technology.InfrastructureUnlock)
        {
            // Foundation hook for future infrastructure unlock effects.
        }

        RecalculateTechnologyState(uid, component);
        // Orion-End
        Dirty(uid, component);

        var ev = new TechnologyDatabaseModifiedEvent(technology.RecipeUnlocks); // Goobstation - Lathe message on recipes update
        RaiseLocalEvent(uid, ref ev);
    }

    /// <summary>
    ///     Returns whether a technology can be unlocked on this database,
    ///     taking parent technologies into account.
    /// </summary>
    /// <returns>Whether it could be unlocked or not</returns>
    private bool CanServerUnlockTechnology(EntityUid uid, TechnologyPrototype technology, out List<ResearchPointAmount> finalCosts, TechnologyDatabaseComponent? database = null, ResearchClientComponent? client = null) // Orion-Edit: Was public
    {
        // Orion-Start
        finalCosts = technology.PointCosts
            .Select(cost => new ResearchPointAmount
            {
                Type = cost.Type,
                Amount = cost.Amount,
            })
            .ToList();
        // Orion-End

        if (!Resolve(uid, ref client, ref database, false))
            return false;

        if (!TryGetClientServer(uid, out var serverUid, out var serverComp, client)) // Orion-Edit
            return false;

        // Orion-Start
        if (!TryComp<TechnologyDatabaseComponent>(serverUid, out var serverDatabase))
            return false;

        if (!CanUnlockTechnology(serverDatabase, technology))
            return false;

        for (var i = 0; i < finalCosts.Count; i++)
        {
            if (finalCosts[i].Type != "General")
                continue;

            var updated = finalCosts[i];
            updated.Amount = GetTechnologyFinalCost(serverDatabase, technology);
            finalCosts[i] = updated;
            break;
        }

        return HasSufficientPoints(serverUid.Value, finalCosts, serverComp);
        // Orion-End
    }

    // Orion-Start
    public override bool CanUnlockTechnology(TechnologyDatabaseComponent component, TechnologyPrototype tech)
    {
        if (!component.VisibleTechnologies.Contains(tech.ID))
        {
            _sawmill.Debug($"Cannot unlock {tech.ID}: not visible.");
            return false;
        }

        if (component.ResearchedTechnologies.Contains(tech.ID))
        {
            _sawmill.Debug($"Cannot unlock {tech.ID}: already researched.");
            return false;
        }

        if (!tech.AllRequiredTechnologies.All(prereq => component.ResearchedTechnologies.Contains(prereq)))
        {
            _sawmill.Debug($"Cannot unlock {tech.ID}: prerequisites not completed.");
            return false;
        }

        if (!HasRequiredExperiments(component, tech))
        {
            _sawmill.Debug($"Cannot unlock {tech.ID}: required experiments not completed.");
            return false;
        }

        if (!component.AvailableTechnologies.Contains(tech.ID))
        {
            _sawmill.Debug($"Cannot unlock {tech.ID}: tech is not available.");
            return false;
        }

        return true;
    }
    // Orion-End

    private void OnDatabaseRegistrationChanged(EntityUid uid, TechnologyDatabaseComponent component, ref ResearchRegistrationChangedEvent args)
    {
        if (args.Server != null)
            return;
        component.MainDiscipline = null;
        component.CurrentTechnologyCards = new List<string>();
        component.SupportedDisciplines = new List<ProtoId<TechDisciplinePrototype>>();
        // Orion-Start
        component.VisibleTechnologies = new List<ProtoId<TechnologyPrototype>>();
        component.AvailableTechnologies = new List<ProtoId<TechnologyPrototype>>();
        component.ResearchedTechnologies = new List<ProtoId<TechnologyPrototype>>();
        component.AvailableExperiments = new List<string>();
        component.UnlockedExperiments = new List<string>();
        component.ActiveExperiments = new List<string>();
        component.CompletedExperiments = new List<string>();
        component.SkippedExperiments = new List<string>();
        component.ExperimentProgress = new List<ResearchExperimentProgress>();
        // Orion-End
        component.UnlockedRecipes = new List<ProtoId<LatheRecipePrototype>>();
        // Orion-Start
        component.RevealedTechnologies = new List<ProtoId<TechnologyPrototype>>();
        component.DiscoveryProgress = new List<TechnologyDiscoveryProgress>();
        component.UnlockedInfrastructure = new List<string>();
        // Orion-End
        Dirty(uid, component);
    }
}
