using Content.Server.GameTicking.Events;
using Content.Shared._Orion.Time.Components;
using Content.Shared.CCVar;
using Robust.Server.GameStates;
using Robust.Shared.Configuration;
using Robust.Shared.Random;

namespace Content.Server._Orion.Time;

//
// License-Identifier: AGPL-3.0-or-later
//

public sealed class TimeSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;

    private int _yearOffset;
    private int _staticYear;
    private bool _useStaticYear;

    private TimeSpan _stationTime;
    private float _accumulatedSeconds;

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(CCVars.StationTimeOffsetYears, v => _yearOffset = v, true);
        _cfg.OnValueChanged(CCVars.StationTimeUseStaticYear, v => _useStaticYear = v, true);
        _cfg.OnValueChanged(CCVars.StationTimeStaticYear, v => _staticYear = v, true);

        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
        SubscribeLocalEvent<StationTimeManagerComponent, MapInitEvent>(OnMapInit);
    }

    private void OnRoundStart(RoundStartingEvent ev)
    {
        _stationTime = TimeSpan.FromHours(_robustRandom.NextFloat(0, 24));
        var stationTimeEntity = Spawn();
        var comp = AddComp<StationTimeManagerComponent>(stationTimeEntity);

        _pvsOverride.AddGlobalOverride(stationTimeEntity);
        UpdateStationTimeComponent(comp);

        Dirty(stationTimeEntity, comp);
    }

    private void OnMapInit(Entity<StationTimeManagerComponent> ent, ref MapInitEvent args)
    {
        UpdateStationTimeComponent(ent.Comp);
        Dirty(ent, ent.Comp);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _accumulatedSeconds += frameTime * 2f; // x2 speed

        if (_accumulatedSeconds < 1.0f)
            return;

        var deltaSeconds = Math.Floor(_accumulatedSeconds);
        _accumulatedSeconds -= (float)deltaSeconds;
        _stationTime += TimeSpan.FromSeconds(deltaSeconds);

        if (_stationTime >= TimeSpan.FromDays(1))
            _stationTime = _stationTime.Subtract(TimeSpan.FromDays(1));
        else if (_stationTime < TimeSpan.Zero)
            _stationTime = _stationTime.Add(TimeSpan.FromDays(1));

        var query = EntityQueryEnumerator<StationTimeManagerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            UpdateStationTimeComponent(comp);
            Dirty(uid, comp);
        }
    }

    private void UpdateStationTimeComponent(StationTimeManagerComponent comp)
    {
        comp.StationTime = _stationTime;
        comp.StationDate = GetStationDate();
    }

    private DateTime GetCurrentStationDate()
    {
        var today = DateTime.UtcNow.Date;

        int stationYear;
        if (_useStaticYear)
        {
            stationYear = _staticYear; // Static year
        }
        else
        {
            stationYear = today.Year + _yearOffset; // Dynamic year
        }

        var day = Math.Min(today.Day, DateTime.DaysInMonth(stationYear, today.Month));
        var stationDate = new DateTime(stationYear, today.Month, day);

        return stationDate;
    }

    public TimeSpan GetStationTime()
    {
        return _stationTime;
    }

    public DateTime GetStationDate()
    {
        return GetCurrentStationDate();
    }
}
