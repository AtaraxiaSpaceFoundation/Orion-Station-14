// SPDX-FileCopyrightText: 2021 20kdc <asdd2808@gmail.com>
// SPDX-FileCopyrightText: 2021 Vera Aguilera Puerto <gradientvera@outlook.com>
// SPDX-FileCopyrightText: 2022 Jezithyr <Jezithyr@gmail.com>
// SPDX-FileCopyrightText: 2022 mirrorcult <lunarautomaton6@gmail.com>
// SPDX-FileCopyrightText: 2022 wrexbe <81056464+wrexbe@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Jezithyr <jezithyr@gmail.com>
// SPDX-FileCopyrightText: 2023 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 ShadowCommander <10494922+ShadowCommander@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 metalgearsloth <comedian_vs_clown@hotmail.com>
// SPDX-FileCopyrightText: 2024 0x6273 <0x40@keemail.me>
// SPDX-FileCopyrightText: 2024 Krunklehorn <42424291+Krunklehorn@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2024 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 gluesniffler <linebarrelerenthusiast@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Body.Components;
using Content.Server.Ghost.Components;
using Content.Shared._Shitmed.Body.Organ;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Systems;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Pointing;
using Content.Goobstation.Shared.Changeling.Components;
using Content.Server.Mobs;
using Content.Shared.Examine;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;

namespace Content.Server.Body.Systems
{
    public sealed class BrainSystem : EntitySystem
    {
        [Dependency] private readonly SharedMindSystem _mindSystem = default!;
        [Dependency] private readonly SharedBodySystem _bodySystem = default!; // Shitmed Change
        [Dependency] private readonly MetaDataSystem _metaData = default!; // Orion
        [Dependency] private readonly MobStateSystem _mobState = default!; // Orion
        [Dependency] private readonly DeathgaspSystem _deathgasp = default!; // Orion

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BrainComponent, OrganAddedToBodyEvent>(HandleAddition);
        // Shitmed Change Start
            SubscribeLocalEvent<BrainComponent, OrganRemovedFromBodyEvent>(HandleRemoval);
            SubscribeLocalEvent<BrainComponent, PointAttemptEvent>(OnPointAttempt);
            SubscribeLocalEvent<DebrainedComponent, ExaminedEvent>(OnBodyExamined); // Orion
        }

        private void HandleRemoval(EntityUid uid, BrainComponent brain, ref OrganRemovedFromBodyEvent args)
        {
            if (TerminatingOrDeleted(uid)
                || TerminatingOrDeleted(args.OldBody)
                || HasComp<ChangelingIdentityComponent>(args.OldBody))
                return;

            brain.Active = false;
            if (!CheckOtherBrains(args.OldBody))
            {
                TryRenameBrain((uid, brain), args.OldBody); // Orion
                // Prevents revival, should kill the user within a given timespan too.
                EnsureComp<DebrainedComponent>(args.OldBody);
                // Orion-Start: Debraining should instantly kill
                if (!_mobState.IsDead(args.OldBody))
                {
                    _mobState.ChangeMobState(args.OldBody, MobState.Dead);
                    _deathgasp.Deathgasp(args.OldBody);
                }
                // Orion-End
                HandleMind(uid, args.OldBody);
            }
        }

        private void HandleAddition(EntityUid uid, BrainComponent brain, ref OrganAddedToBodyEvent args)
        {
            if (TerminatingOrDeleted(uid)
                || TerminatingOrDeleted(args.Body)
                || HasComp<ChangelingIdentityComponent>(args.Body))
                return;

            if (!CheckOtherBrains(args.Body))
            {
                TryRenameBrain((uid, brain), args.Body); // Orion
                RemComp<DebrainedComponent>(args.Body);
                HandleMind(args.Body, uid, brain);
            }
        }


        private void HandleMind(EntityUid newEntity, EntityUid oldEntity, BrainComponent? brain = null)
        {
            if (TerminatingOrDeleted(newEntity) || TerminatingOrDeleted(oldEntity))
                return;

            EnsureComp<MindContainerComponent>(newEntity);
            EnsureComp<MindContainerComponent>(oldEntity);

            var ghostOnMove = EnsureComp<GhostOnMoveComponent>(newEntity);
            if (HasComp<BodyComponent>(newEntity))
                ghostOnMove.MustBeDead = true;

            if (!_mindSystem.TryGetMind(oldEntity, out var mindId, out var mind))
                return;

            _mindSystem.TransferTo(mindId, newEntity, mind: mind);
            // Orion-Edit-Start
            if (brain != null)
                brain.Active = true;
            // Orion-Edit-End
        }

        private bool CheckOtherBrains(EntityUid entity)
        {
            var hasOtherBrains = false;
            if (TryComp<BodyComponent>(entity, out var body))
            {
                if (TryComp<BrainComponent>(entity, out _))
                    hasOtherBrains = true;
                else
                {
                    foreach (var (organ, _) in _bodySystem.GetBodyOrgans(entity, body))
                    {
                        if (TryComp<BrainComponent>(organ, out var brain) && brain.Active)
                        {
                            hasOtherBrains = true;
                            break;
                        }
                    }
                }
            }

            return hasOtherBrains;
        }

        // Shitmed Change End
        private void OnPointAttempt(Entity<BrainComponent> ent, ref PointAttemptEvent args)
        {
            args.Cancel();
        }

        // Orion-Start
        private void TryRenameBrain(Entity<BrainComponent> ent, EntityUid playerEntity)
        {
            if (ent.Comp.Renamed || TerminatingOrDeleted(ent))
                return;

            var newName = Loc.GetString("comp-brain-name", ("name", Name(ent)), ("player", Name(playerEntity)));
            _metaData.SetEntityName(ent, newName);

            ent.Comp.Renamed = true;
        }

        private void OnBodyExamined(Entity<DebrainedComponent> ent, ref ExaminedEvent args)
        {
            args.PushMarkup(Loc.GetString("comp-brain-examine-debrained", ("entity", ent)));
        }
        // Orion-End
    }
}
