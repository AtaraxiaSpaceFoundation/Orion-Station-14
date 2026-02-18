// SPDX-FileCopyrightText: 2025 Coenx-flex
// SPDX-FileCopyrightText: 2025 Cojoke
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using Content.Shared._Orion.CorticalBorer.Components;
using Content.Shared.Body.Part;
using Content.Shared.Examine;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Robust.Server.Containers;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server._Orion.CorticalBorer;

public sealed class CorticalBorerInfestedSystem : EntitySystem
{
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly CorticalBorerSystem _borer = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<CorticalBorerInfestedComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<CorticalBorerInfestedComponent, ExaminedEvent>(OnExaminedInfested);

        SubscribeLocalEvent<CorticalBorerInfestedComponent, BodyPartRemovedEvent>(OnBodyPartRemoved);
        SubscribeLocalEvent<CorticalBorerInfestedComponent, MobStateChangedEvent>(OnStateChange);
        SubscribeLocalEvent<CorticalBorerInfestedComponent, MindRemovedMessage>(OnMindRemoved);
        SubscribeLocalEvent<CorticalBorerInfestedComponent, EntityTerminatingEvent>(OnHostTerminating);
    }

    private void OnInit(Entity<CorticalBorerInfestedComponent> infested, ref MapInitEvent args)
    {
        infested.Comp.ControlContainer = _container.EnsureContainer<Container>(infested, "ControlContainer");
        infested.Comp.InfestationContainer = _container.EnsureContainer<Container>(infested, "InfestationContainer");
    }

    private void OnExaminedInfested(Entity<CorticalBorerInfestedComponent> infected, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (!TryGetBorer(infected, out var borerComp) || !borerComp.ControllingHost)
            return;

        args.PushMarkup(Loc.GetString("cortical-borer-infested-examine"));

        if (args.Examined != args.Examiner)
            return;

        if (infected.Comp.ControlTimeEnd is { } cte)
        {
            var timeRemaining = Math.Floor((cte - _timing.CurTime).TotalSeconds);
            args.PushMarkup(Loc.GetString("infested-control-examined", ("timeremaining", timeRemaining)));
        }

        args.PushMarkup(Loc.GetString("cortical-borer-self-examine", ("chempoints", borerComp.ChemicalPoints)));
    }

    private void EndControlAndEject(Entity<CorticalBorerInfestedComponent> infected)
    {
        if (!TryGetBorer(infected, out var _))
            return;

        _borer.EndControl(infected);
        _borer.TryEjectBorer(infected.Comp.Borer);
    }

    private void OnStateChange(Entity<CorticalBorerInfestedComponent> infected, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        if (TryGetBorer(infected, out var borerComp) && borerComp.ControllingHost)
            _borer.EndControl(infected);
    }

    private void OnBodyPartRemoved(Entity<CorticalBorerInfestedComponent> infected, ref BodyPartRemovedEvent args)
    {
        if (!TryComp<BodyPartComponent>(args.Part, out var part) ||
            part.PartType != BodyPartType.Head)
            return;

        EndControlAndEject(infected);
    }

    private void OnMindRemoved(Entity<CorticalBorerInfestedComponent> infected, ref MindRemovedMessage args)
    {
        if (!TryGetBorer(infected, out var borerComp) || !borerComp.ControllingHost)
            return;

        EndControlAndEject(infected);
    }

    private void OnHostTerminating(Entity<CorticalBorerInfestedComponent> infected, ref EntityTerminatingEvent args)
    {
        if (TerminatingOrDeleted(infected.Comp.Borer))
            return;

        _borer.HandleHostTerminating(infected);
    }

    private bool TryGetBorer(Entity<CorticalBorerInfestedComponent> infected, [NotNullWhen(true)] out CorticalBorerComponent? borerComp)
    {
        borerComp = null;

        if (TerminatingOrDeleted(infected.Comp.Borer))
            return false;

        return TryComp(infected.Comp.Borer, out borerComp);
    }
}
