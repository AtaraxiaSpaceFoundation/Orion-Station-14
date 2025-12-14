using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Random;

namespace Content.Shared._Orion.Time;

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

    private TimeSpan _stationTime; // Handle time
    private float _accumulatedSeconds;

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(CCVars.StationTimeOffsetYears, v => _yearOffset = v, true);
        _cfg.OnValueChanged(CCVars.StationTimeUseStaticYear, v => _useStaticYear = v, true);
        _cfg.OnValueChanged(CCVars.StationTimeStaticYear, v => _staticYear = v, true);

        _stationTime = TimeSpan.FromHours(_robustRandom.NextFloat(0, 24));
    }

    // Accumulate time faster
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
    }

    public DateTime GetStationDate()
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
}
