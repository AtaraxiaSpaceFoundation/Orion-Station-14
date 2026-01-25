// SPDX-FileCopyrightText: 2021 Alex Evgrashin <aevgrashin@yandex.ru>
// SPDX-FileCopyrightText: 2021 Paul Ritter <ritter.paul1@googlemail.com>
// SPDX-FileCopyrightText: 2021 metalgearsloth <comedian_vs_clown@hotmail.com>
// SPDX-FileCopyrightText: 2022 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 Pieter-Jan Briers <pieterjan.briers+git@gmail.com>
// SPDX-FileCopyrightText: 2022 Vera Aguilera Puerto <6766154+Zumorica@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 mirrorcult <lunarautomaton6@gmail.com>
// SPDX-FileCopyrightText: 2022 wrexbe <81056464+wrexbe@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 chromiumboy <50505512+chromiumboy@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: MIT

using Content.Shared.Medical.SuitSensor;
using Robust.Shared.Audio;

namespace Content.Server.Medical.CrewMonitoring;

[RegisterComponent]
[Access(typeof(CrewMonitoringConsoleSystem))]
public sealed partial class CrewMonitoringConsoleComponent : Component
{
    /// <summary>
    ///     List of all currently connected sensors to this console.
    /// </summary>
    public Dictionary<string, SuitSensorStatus> ConnectedSensors = new();

    /// <summary>
    ///     After what time sensor consider to be lost.
    /// </summary>
    [DataField("sensorTimeout"), ViewVariables(VVAccess.ReadWrite)]
    public float SensorTimeout = 10f;

    // Orion-Start
    /// <summary>
    ///     Enable or disable alerts.
    /// </summary>
    [DataField]
    public bool DoAlert;

    /// <summary>
    ///     Time interval between alerts in seconds.
    /// </summary>
    [DataField]
    public float AlertTime = 30f;

    /// <summary>
    ///     Sound to play when alert is triggered.
    /// </summary>
    [DataField]
    public SoundSpecifier AlertSound = new SoundPathSpecifier("/Audio/_Orion/Machines/crew_monitoring_alert.ogg");

    [DataField]
    public AudioParams AlertAudioParams = AudioParams.Default.WithVolume(-8f).WithVariation(0.25f);

    /// <summary>
    ///     Timestamp of the last played alert.
    /// </summary>
    [ViewVariables]
    public TimeSpan NextAlertTime = TimeSpan.Zero;

    [DataField(serverOnly: true)]
    public Color? NormalLightColor { get; set; }

    [DataField(serverOnly: true)]
    public float? NormalLightEnergy { get; set; }

    [DataField(serverOnly: true)]
    public float? NormalLightRadius { get; set; }
    // Orion-End
}
