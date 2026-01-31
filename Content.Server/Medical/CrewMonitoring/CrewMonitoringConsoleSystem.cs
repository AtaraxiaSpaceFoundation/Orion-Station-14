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
using Content.Server.Jittering;
using Content.Server.Power.EntitySystems;
using Content.Server.PowerCell;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Bed.Components;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Jittering;
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
    [Dependency] private readonly JitteringSystem _jitter = default!;
    [Dependency] private readonly SharedPointLightSystem _light = default!;
    [Dependency] private readonly ContainerSystem _containerSystem = default!;
    // Orion-End

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, BoundUIOpenedEvent>(OnUIOpened);
    }

    // Orion-Start
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var component in EntityQuery<CrewMonitoringConsoleComponent>())
        {
            if (_gameTiming.CurTime < component.NextAlertTime)
                continue;

            if (!component.DoAlert)
                continue;

            var uid = component.Owner;

            if (!this.IsPowered(uid, EntityManager))
            {
                RemCompDeferred<JitteringComponent>(uid);
                continue;
            }

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
            _jitter.AddJitter(uid, 10, 15);
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

            RemCompDeferred<JitteringComponent>(uid);
        }
    }

    private bool HasUnsecuredCorpse(CrewMonitoringConsoleComponent component)
    {
        foreach (var sensor in component.ConnectedSensors.Values)
        {
            // Check for corpses with coordinates sensor mode
            if (sensor.IsAlive || sensor.Coordinates == null)
                continue;

            if (!TryGetEntity(sensor.OwnerUid, out var corpse) || Deleted(corpse.Value))
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
        // GoobStation - Start
        var isCommandOnly = HasComp<CrewMonitorScanningComponent>(uid);

        var filteredSensors = component.ConnectedSensors
            .Where(pair => isCommandOnly
                ? pair.Value.IsCommandTracker
                : !pair.Value.IsCommandTracker)
            .Select(pair => pair.Value)
            .ToList();
        _uiSystem.SetUiState(uid, CrewMonitoringUIKey.Key, new CrewMonitoringState(filteredSensors));
        // GoobStation - End
        //var allSensors = component.ConnectedSensors.Values.ToList();
        //_uiSystem.SetUiState(uid, CrewMonitoringUIKey.Key, new CrewMonitoringState(allSensors));
    }
}
