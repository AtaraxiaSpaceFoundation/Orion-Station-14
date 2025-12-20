using Content.Server.GameTicking.Events;
using Content.Server.Station.Events;
using Content.Shared._Orion.Time.Components;
using Content.Shared.CCVar;
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
        SubscribeLocalEvent<MapInitEvent>(OnMapInit);
    }

    private void OnRoundStart(RoundStartingEvent ev)
    {
        var manager = Spawn();
        AddComp<StationTimeComponent>(manager);
    }

    private void OnMapInit(Entity<StationTimeComponent> ent, ref MapInitEvent args)
    {
        _stationTime = TimeSpan.FromHours(_robustRandom.NextFloat(0, 24));
        UpdateStationTimeComponent(manager);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _accumulatedSeconds += frameTime * 2f; // x2 speed

        var delta = TimeSpan.FromSeconds(_accumulatedSeconds);
        _stationTime += delta;

        if (_stationTime >= TimeSpan.FromDays(1))
            _stationTime = _stationTime.Subtract(TimeSpan.FromDays(1));
        else if (_stationTime < TimeSpan.Zero)
            _stationTime = _stationTime.Add(TimeSpan.FromDays(1));

        _accumulatedSeconds -= (float)delta.TotalSeconds;

        var query = EntityQueryEnumerator<StationTimeComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            UpdateStationTimeComponent(comp);
            Dirty(uid, comp);
        }
    }

    private void UpdateStationTimeComponent(StationTimeComponent comp)
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
