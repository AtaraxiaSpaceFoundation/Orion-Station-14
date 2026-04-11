// SPDX-FileCopyrightText: 2021 Alex Evgrashin <aevgrashin@yandex.ru>
// SPDX-FileCopyrightText: 2021 Paul Ritter <ritter.paul1@googlemail.com>
// SPDX-FileCopyrightText: 2022 Acruid <shatter66@gmail.com>
// SPDX-FileCopyrightText: 2022 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 metalgearsloth <comedian_vs_clown@hotmail.com>
// SPDX-FileCopyrightText: 2022 mirrorcult <lunarautomaton6@gmail.com>
// SPDX-FileCopyrightText: 2023 Julian Giebel <juliangiebel@live.de>
// SPDX-FileCopyrightText: 2023 TemporalOroboros <TemporalOroboros@gmail.com>
// SPDX-FileCopyrightText: 2023 chromiumboy <50505512+chromiumboy@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 keronshb <54602815+keronshb@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 themias <89101928+themias@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Baptr0b0t <152836416+Baptr0b0t@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 Ted Lukin <66275205+pheenty@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 pheenty <fedorlukin2006@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Shared.CrewMonitoring;
using Content.Server.Power.EntitySystems;
using Content.Server.PowerCell;
using Content.Server.Popups;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Bed.Components;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Emag.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Medical.CrewMonitoring;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Morgue.Components;
using Content.Shared.Pinpointer;
using Robust.Server.Audio;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server.Medical.CrewMonitoring;

public sealed class CrewMonitoringConsoleSystem : EntitySystem
{
    [Dependency] private readonly PowerCellSystem _cell = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    // Orion-Start
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedPointLightSystem _light = default!;
    [Dependency] private readonly ContainerSystem _containerSystem = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    // Orion-End

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, BoundUIOpenedEvent>(OnUIOpened);
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, GotEmaggedEvent>(OnEmagged); // Orion
    }

    // Orion-Start
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CrewMonitoringConsoleComponent>();
        while (query.MoveNext(out var uid, out var component))
        {

            if (component.EmagExpireTime.HasValue && _gameTiming.CurTime >= component.EmagExpireTime.Value) // Emag expiry timer
            {
                component.EmagExpireTime = null;
                UpdateUserInterface(uid, component);
            }

            if (!this.IsPowered(uid, EntityManager))
                continue;

            if (_gameTiming.CurTime < component.NextAlertTime)
                continue;

            if (!component.DoAlert)
                continue;

            var hasUnsecuredCorpse = HasUnsecuredCorpse(component);
            TriggerAlert(uid, component, hasUnsecuredCorpse);
        }
    }

    private void TriggerAlert(EntityUid uid, CrewMonitoringConsoleComponent component, bool hasCorpse)
    {
        component.NextAlertTime = _gameTiming.CurTime + TimeSpan.FromSeconds(component.AlertTime);

        if (hasCorpse)
        {
            if (TryComp(uid, out PointLightComponent? light))
            {
                component.NormalLightColor ??= light.Color;
                component.NormalLightEnergy ??= light.Energy;
                component.NormalLightRadius ??= light.Radius;

                _light.SetColor(uid, Color.Red, light);
                _light.SetEnergy(uid, 40, light);
                _light.SetRadius(uid, 1.5f, light);
            }

            _audio.PlayPvs(component.AlertSound, uid, component.AlertAudioParams);
        }
        else
        {
            if (TryComp(uid, out PointLightComponent? light))
            {
                if (component.NormalLightColor != null)
                    _light.SetColor(uid, component.NormalLightColor.Value, light);
                if (component.NormalLightEnergy != null)
                    _light.SetEnergy(uid, component.NormalLightEnergy.Value, light);
                if (component.NormalLightRadius != null)
                    _light.SetRadius(uid, component.NormalLightRadius.Value, light);
            }
        }
    }

    private bool HasUnsecuredCorpse(CrewMonitoringConsoleComponent component)
    {
        IEnumerable<SuitSensorStatus> sensors = component.ConnectedSensors.Values;

        if (component.Departments.Count > 0)
        {
            var allowed = component.Departments.Select(d => d.ToString()).ToHashSet();
            sensors = sensors.Where(s => s.JobDepartments.Any(dep => allowed.Contains(dep)));
        }

        foreach (var s in sensors)
        {
            if (s.Mode != SuitSensorMode.SensorCords)
                continue;

            if (s.IsAlive || s.Coordinates == null)
                continue;

            if (!TryGetEntity(s.OwnerUid, out var corpse) || Deleted(corpse.Value))
                continue;

            if (!IsCorpseSecured(corpse.Value))
                return true;
        }

        return false;
    }

    private bool IsCorpseSecured(EntityUid entity)
    {
        // If secured in a morgue or something that freezes rotting - secured
        if (_containerSystem.TryGetContainingContainer(entity, out var container) &&
            (HasComp<MorgueComponent>(container.Owner) || HasComp<AntiRottingContainerComponent>(container.Owner)))
            return true;

        // If buckled in a stasis bed - secured
        if (HasComp<StasisBedBuckledComponent>(entity))
            return true;

        return false;
    }
    // Orion-End

    private void OnRemove(EntityUid uid, CrewMonitoringConsoleComponent component, ComponentRemove args)
    {
        component.ConnectedSensors.Clear();
    }

    private void OnPacketReceived(EntityUid uid, CrewMonitoringConsoleComponent component, DeviceNetworkPacketEvent args)
    {
        var payload = args.Data;

        // Check command
        if (!payload.TryGetValue(DeviceNetworkConstants.Command, out string? command))
            return;

        if (command != DeviceNetworkConstants.CmdUpdatedState)
            return;

        if (!payload.TryGetValue(SuitSensorConstants.NET_STATUS_COLLECTION, out Dictionary<string, SuitSensorStatus>? sensorStatus))
            return;
        component.ConnectedSensors = sensorStatus;

        UpdateUserInterface(uid, component);
    }

    private void OnUIOpened(EntityUid uid, CrewMonitoringConsoleComponent component, BoundUIOpenedEvent args)
    {
        if (!_cell.TryUseActivatableCharge(uid))
            return;

        UpdateUserInterface(uid, component);
    }

    private void UpdateUserInterface(EntityUid uid, CrewMonitoringConsoleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!_uiSystem.IsUiOpen(uid, CrewMonitoringUIKey.Key))
            return;

        // The grid must have a NavMapComponent to visualize the map in the UI
        var xform = Transform(uid);

        if (xform.GridUid != null)
            EnsureComp<NavMapComponent>(xform.GridUid.Value);

        // Update all sensors info
        // Orion-Start: Filtering by departments
        var allSensors = component.ConnectedSensors.Values.ToList();

        if (component.Departments.Count > 0)
        {
            var allowed = component.Departments.Select(d => d.ToString()).ToHashSet();
            allSensors = allSensors
                .Where(s => s.JobDepartments.Any(dep => allowed.Contains(dep)))
                .Select(s => new SuitSensorStatus(
                    s.OwnerUid,
                    s.SuitSensorUid,
                    s.Name,
                    s.Job,
                    s.JobIcon,
                    s.JobDepartments!.Where(dep => allowed.Contains(dep)).ToList(),
                    s.Mode)
                {
                    IsAlive = s.IsAlive,
                    TotalDamage = s.TotalDamage,
                    TotalDamageThreshold = s.TotalDamageThreshold,
                    Coordinates = s.Coordinates,
                    IsCommandTracker = s.IsCommandTracker,
                    Timestamp = s.Timestamp,
                })
                .ToList();
        }

        var temporaryEmagged = component.EmagExpireTime is { } expireAt && _gameTiming.CurTime < expireAt;
        var effectiveIsEmagged = component.IsEmagged || temporaryEmagged; // For Emag
        if (!effectiveIsEmagged)
        {
            allSensors = allSensors
                .Where(s => s.Mode != SuitSensorMode.SensorOff)
                .Select(s =>
                {
                    var p = new SuitSensorStatus(s.OwnerUid, s.SuitSensorUid, s.Name, s.Job, s.JobIcon, s.JobDepartments, s.Mode)
                    {
                        IsAlive = s.IsAlive,
                        IsCommandTracker = s.IsCommandTracker,
                        Timestamp = s.Timestamp,
                    };
                    switch (s.Mode)
                    {
                        case SuitSensorMode.SensorVitals:
                            p.TotalDamage = s.TotalDamage;
                            p.TotalDamageThreshold = s.TotalDamageThreshold;
                            break;
                        case SuitSensorMode.SensorCords:
                            p.TotalDamage = s.TotalDamage;
                            p.TotalDamageThreshold = s.TotalDamageThreshold;
                            p.Coordinates = s.Coordinates;
                            break;
                    }
                    return p;
                })
                .ToList();
        }
        // Orion-End
        // GoobStation - Start
        var isCommandOnly = HasComp<CrewMonitorScanningComponent>(uid);

        // Orion-Edit-Start: use allSensors (already department-filtered) instead of ConnectedSensors
        var filteredSensors = allSensors
            .Where(s => isCommandOnly
                ? s.IsCommandTracker
                : !s.IsCommandTracker)
            .ToList();
        _uiSystem.SetUiState(uid, CrewMonitoringUIKey.Key, new CrewMonitoringState(filteredSensors, effectiveIsEmagged));
        // Orion-Edit-End
        // GoobStation - End
        //var allSensors = component.ConnectedSensors.Values.ToList();
        //_uiSystem.SetUiState(uid, CrewMonitoringUIKey.Key, new CrewMonitoringState(allSensors));
    }

    // Orion-Start
    private void OnEmagged(EntityUid uid, CrewMonitoringConsoleComponent component, ref GotEmaggedEvent ev)
    {
        var temporaryEmagged = component.EmagExpireTime is { } expireAt && _gameTiming.CurTime < expireAt;
        if (ev.Handled || component.IsEmagged || temporaryEmagged)
            return;

        _audio.PlayPvs(component.SparkSound, uid);
        _popup.PopupEntity(
            Loc.GetString("emag-success", ("target", Identity.Entity(uid, EntityManager))),
            uid);

        component.EmagExpireTime = _gameTiming.CurTime + CrewMonitoringConsoleComponent.EmagDuration;
        UpdateUserInterface(uid, component);
        ev.Handled = true;
    }
    // Orion-End
}
