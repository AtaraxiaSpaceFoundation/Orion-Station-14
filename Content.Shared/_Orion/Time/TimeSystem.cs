using Content.Shared.CCVar;
using Robust.Shared.Configuration;

namespace Content.Shared._Orion.Time;

//
// License-Identifier: AGPL-3.0-or-later
//

public sealed class TimeSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private int _yearOffset;
    private int _staticYear;
    private bool _useStaticYear;

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(CCVars.StationTimeOffsetYears, v => _yearOffset = v, true);
        _cfg.OnValueChanged(CCVars.StationTimeUseStaticYear, v => _useStaticYear = v, true);
        _cfg.OnValueChanged(CCVars.StationTimeStaticYear, v => _staticYear = v, true);
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

    public TimeSpan GetStationTime() // TODO: Randomize this?
    {
        return DateTime.UtcNow.TimeOfDay;
    }
}
