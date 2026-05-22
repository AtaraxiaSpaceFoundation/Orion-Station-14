// SPDX-FileCopyrightText: 2023 Josh Bothun <joshbothun@gmail.com>
// SPDX-FileCopyrightText: 2023 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 TemporalOroboros <TemporalOroboros@gmail.com>
// SPDX-FileCopyrightText: 2023 metalgearsloth <comedian_vs_clown@hotmail.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 whateverusername0 <whateveremail>
//
// SPDX-License-Identifier: MIT

using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared._Orion.Construction;
using Content.Shared._Orion.Construction.Events;
using Content.Shared.Power;
using Content.Shared.Rounding;
using Content.Shared.SMES;
using JetBrains.Annotations;
using Robust.Shared.Timing;

namespace Content.Server.Power.SMES;

[UsedImplicitly]
public sealed class SmesSystem : EntitySystem // goob edit - made public
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(PowerNetSystem));

        SubscribeLocalEvent<SmesComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SmesComponent, ChargeChangedEvent>(OnBatteryChargeChanged);
        // Orion-Start
        SubscribeLocalEvent<SmesComponent, RefreshPartsEvent>(OnPartsRefresh);
        SubscribeLocalEvent<SmesComponent, UpgradeExamineEvent>(OnUpgradeExamine);
        // Orion-End
    }

    private void OnMapInit(EntityUid uid, SmesComponent component, MapInitEvent args)
    {
        // Orion-Start
        if (TryComp<PowerNetworkBatteryComponent>(uid, out var netBattery))
        {
            component.BaseMaxSupply = netBattery.MaxSupply;
            component.BaseMaxChargeRate = netBattery.MaxChargeRate;
            component.FinalMaxSupply = netBattery.MaxSupply;
            component.FinalMaxChargeRate = netBattery.MaxChargeRate;
        }
        // Orion-End

        UpdateSmesState(uid, component);
    }

    private void OnBatteryChargeChanged(EntityUid uid, SmesComponent component, ref ChargeChangedEvent args)
    {
        UpdateSmesState(uid, component);
    }

    // Orion-Start
    private void OnPartsRefresh(EntityUid uid, SmesComponent component, RefreshPartsEvent args)
    {
        if (!TryComp<PowerNetworkBatteryComponent>(uid, out var netBattery))
            return;

        var rating = Math.Max(1f, args.GetPartRatingSum(MachinePartIds.Capacitor));
        component.FinalMaxSupply = component.BaseMaxSupply * rating;
        component.FinalMaxChargeRate = component.BaseMaxChargeRate * rating;
        netBattery.MaxSupply = component.FinalMaxSupply;
        netBattery.MaxChargeRate = component.FinalMaxChargeRate;

        UpdateSmesState(uid, component);
    }

    private static void OnUpgradeExamine(EntityUid uid, SmesComponent component, UpgradeExamineEvent args)
    {
        var inputMultiplier = component.BaseMaxChargeRate <= 0f
            ? 1f
            : component.FinalMaxChargeRate / component.BaseMaxChargeRate;
        var outputMultiplier = component.BaseMaxSupply <= 0f
            ? 1f
            : component.FinalMaxSupply / component.BaseMaxSupply;

        args.AddPercentageUpgrade("machine-upgrade-power-input", inputMultiplier);
        args.AddPercentageUpgrade("machine-upgrade-power-output", outputMultiplier);
    }
    // Orion-End

    private void UpdateSmesState(EntityUid uid, SmesComponent smes)
    {
        var newLevel = CalcChargeLevel(uid);
        if (newLevel != smes.LastChargeLevel && smes.LastChargeLevelTime + smes.VisualsChangeDelay < _gameTiming.CurTime)
        {
            smes.LastChargeLevel = newLevel;
            smes.LastChargeLevelTime = _gameTiming.CurTime;

            _appearance.SetData(uid, SmesVisuals.LastChargeLevel, newLevel);
        }

        var newChargeState = CalcChargeState(uid);
        if (newChargeState != smes.LastChargeState && smes.LastChargeStateTime + smes.VisualsChangeDelay < _gameTiming.CurTime)
        {
            smes.LastChargeState = newChargeState;
            smes.LastChargeStateTime = _gameTiming.CurTime;

            _appearance.SetData(uid, SmesVisuals.LastChargeState, newChargeState);
        }
    }

    private int CalcChargeLevel(EntityUid uid, BatteryComponent? battery = null)
    {
        if (!Resolve(uid, ref battery, false))
            return 0;

        return ContentHelpers.RoundToLevels(battery.CurrentCharge, battery.MaxCharge, 6);
    }

    private ChargeState CalcChargeState(EntityUid uid, PowerNetworkBatteryComponent? netBattery = null)
    {
        if (!Resolve(uid, ref netBattery, false))
            return ChargeState.Still;

        return (netBattery.CurrentSupply - netBattery.CurrentReceiving) switch
        {
            > 0 => ChargeState.Discharging,
            < 0 => ChargeState.Charging,
            _ => ChargeState.Still
        };
    }
}
