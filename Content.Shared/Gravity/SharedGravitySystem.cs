// SPDX-FileCopyrightText: 2021 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2021 Galactic Chimp <63882831+GalacticChimp@users.noreply.github.com>
// SPDX-FileCopyrightText: 2021 metalgearsloth <comedian_vs_clown@hotmail.com>
// SPDX-FileCopyrightText: 2022 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 metalgearsloth <metalgearsloth@gmail.com>
// SPDX-FileCopyrightText: 2022 wrexbe <81056464+wrexbe@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 TemporalOroboros <TemporalOroboros@gmail.com>
// SPDX-FileCopyrightText: 2024 Adeinitas <147965189+adeinitas@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Aidenkrz <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2024 Danger Revolution! <142105406+DangerRevolution@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Pieter-Jan Briers <pieterjan.briers+git@gmail.com>
// SPDX-FileCopyrightText: 2024 Timemaster99 <57200767+Timemaster99@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 VMSolidus <evilexecutive@gmail.com>
// SPDX-FileCopyrightText: 2024 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Alert;
using Content.Shared.Inventory;
using Content.Shared.Movement.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Content.Shared._EinsteinEngines.Flight; // Goobstation

namespace Content.Shared.Gravity
{
    public abstract partial class SharedGravitySystem : EntitySystem
    {
        [Dependency] protected readonly IGameTiming Timing = default!;
        [Dependency] private readonly AlertsSystem _alerts = default!;

        public static readonly ProtoId<AlertPrototype> WeightlessAlert = "Weightless";

        private EntityQuery<GravityComponent> _gravityQuery;
        private readonly Dictionary<EntityUid, HashSet<EntityUid>> _alertsByGrid = new(); // Orion

        public bool IsWeightless(EntityUid uid, PhysicsComponent? body = null, TransformComponent? xform = null)
        {
            Resolve(uid, ref body, false);

            if ((body?.BodyType & (BodyType.Static | BodyType.Kinematic)) != 0)
                return false;

            if (TryComp<FlightComponent>(uid, out var flying) && flying.On) // Goobstation
                return true;

            if (TryComp<MovementIgnoreGravityComponent>(uid, out var ignoreGravityComponent))
                return ignoreGravityComponent.Weightless;

            var ev = new IsWeightlessEvent(uid);
            RaiseLocalEvent(uid, ref ev);
            if (ev.Handled)
                return ev.IsWeightless;

            if (!Resolve(uid, ref xform))
                return true;

            // If grid / map has gravity
            if (EntityGridOrMapHaveGravity((uid, xform)))
                return false;

            return true;
        }

        /// <summary>
        /// Checks if a given entity is currently standing on a grid or map that supports having gravity at all.
        /// </summary>
        public bool EntityOnGravitySupportingGridOrMap(Entity<TransformComponent?> entity)
        {
            entity.Comp ??= Transform(entity);

            return _gravityQuery.HasComp(entity.Comp.GridUid) ||
                   _gravityQuery.HasComp(entity.Comp.MapUid);
        }


        /// <summary>
        /// Checks if a given entity is currently standing on a grid or map that has gravity of some kind.
        /// </summary>
        public bool EntityGridOrMapHaveGravity(Entity<TransformComponent?> entity)
        {
            entity.Comp ??= Transform(entity);

            return _gravityQuery.TryComp(entity.Comp.GridUid, out var gravity) && gravity.Enabled ||
                   _gravityQuery.TryComp(entity.Comp.MapUid, out var mapGravity) && mapGravity.Enabled;
        }

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<GridInitializeEvent>(OnGridInit);
            SubscribeLocalEvent<GridRemovalEvent>(OnGridRemoved); // Orion
            SubscribeLocalEvent<AlertSyncEvent>(OnAlertsSync);
            // Orion-Start
            SubscribeLocalEvent<AlertsComponent, ComponentInit>(OnAlertsInit);
            SubscribeLocalEvent<AlertsComponent, ComponentRemove>(OnAlertsRemove);
            // Orion-End
            SubscribeLocalEvent<AlertsComponent, EntParentChangedMessage>(OnAlertsParentChange);
            SubscribeLocalEvent<GravityChangedEvent>(OnGravityChange);
            SubscribeLocalEvent<GravityComponent, ComponentGetState>(OnGetState);
            SubscribeLocalEvent<GravityComponent, ComponentHandleState>(OnHandleState);

            _gravityQuery = GetEntityQuery<GravityComponent>();
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            UpdateShake();
        }

        private void OnHandleState(EntityUid uid, GravityComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not GravityComponentState state)
                return;

            if (component.EnabledVV == state.Enabled)
                return;
            component.EnabledVV = state.Enabled;
            var ev = new GravityChangedEvent(uid, component.EnabledVV);
            RaiseLocalEvent(uid, ref ev, true);
        }

        private static void OnGetState(EntityUid uid, GravityComponent component, ref ComponentGetState args) // Orion-Edit: Static
        {
            args.State = new GravityComponentState(component.EnabledVV);
        }

        private void OnGravityChange(ref GravityChangedEvent ev)
        {
            // Orion-Edit-Start
            if (!_alertsByGrid.TryGetValue(ev.ChangedGridIndex, out var entities))
                return;

            foreach (var uid in entities)
            // Orion-Edit-End
            {
                if (Deleted(uid) || Terminating(uid)) // Orion-Edit
                    continue;

                if (!ev.HasGravity)
                {
                    _alerts.ShowAlert(uid, WeightlessAlert);
                }
                else
                {
                    _alerts.ClearAlert(uid, WeightlessAlert);
                }
            }
        }

        private void OnAlertsSync(AlertSyncEvent ev)
        {
            if (IsWeightless(ev.Euid))
            {
                _alerts.ShowAlert(ev.Euid, WeightlessAlert);
            }
            else
            {
                _alerts.ClearAlert(ev.Euid, WeightlessAlert);
            }
        }

        // Orion-Start
        private void OnAlertsInit(EntityUid uid, AlertsComponent component, ComponentInit args)
        {
            TrackAlertsEntity(uid);
        }

        private void OnAlertsRemove(EntityUid uid, AlertsComponent component, ComponentRemove args)
        {
            UntrackAlertsEntity(uid, component.TrackedGridUid);
        }
        // Orion-End

        private void OnAlertsParentChange(EntityUid uid, AlertsComponent component, ref EntParentChangedMessage args)
        {
            // Orion-Start
            var oldGrid = args.OldParent;
            if (oldGrid != null)
            {
                if (TryComp<TransformComponent>(oldGrid.Value, out var oldParentXform))
                    oldGrid = oldParentXform.GridUid ?? oldGrid;
            }

            UntrackAlertsEntity(uid, oldGrid);
            TrackAlertsEntity(uid);
            // Orion-End

            if (IsWeightless(uid))
            {
                _alerts.ShowAlert(uid, WeightlessAlert);
            }
            else
            {
                _alerts.ClearAlert(uid, WeightlessAlert);
            }
        }

        private void OnGridInit(GridInitializeEvent ev)
        {
            EnsureComp<GravityComponent>(ev.EntityUid);
        }

        // Orion-Start
        private void OnGridRemoved(GridRemovalEvent ev)
        {
            _alertsByGrid.Remove(ev.EntityUid);
        }

        private void TrackAlertsEntity(EntityUid uid)
        {
            if (!TryComp<AlertsComponent>(uid, out var alerts))
                return;

            var gridUid = Transform(uid).GridUid;
            alerts.TrackedGridUid = gridUid;
            if (gridUid == null)
                return;

            if (!_alertsByGrid.TryGetValue(gridUid.Value, out var entities))
            {
                entities = new HashSet<EntityUid>();
                _alertsByGrid[gridUid.Value] = entities;
            }

            entities.Add(uid);
        }

        private void UntrackAlertsEntity(EntityUid uid, EntityUid? gridUid)
        {
            if (TryComp<AlertsComponent>(uid, out var alerts) && alerts.TrackedGridUid == gridUid)
                alerts.TrackedGridUid = null;

            if (gridUid == null || !_alertsByGrid.TryGetValue(gridUid.Value, out var entities))
                return;

            entities.Remove(uid);
            if (entities.Count == 0)
                _alertsByGrid.Remove(gridUid.Value);
        }
        // Orion-End

        [Serializable, NetSerializable]
        private sealed class GravityComponentState : ComponentState
        {
            public bool Enabled { get; }

            public GravityComponentState(bool enabled)
            {
                Enabled = enabled;
            }
        }
    }

    [ByRefEvent]
    public record struct IsWeightlessEvent(EntityUid Entity, bool IsWeightless = false, bool Handled = false) : IInventoryRelayEvent
    {
        SlotFlags IInventoryRelayEvent.TargetSlots => ~SlotFlags.POCKET;
    }
}
