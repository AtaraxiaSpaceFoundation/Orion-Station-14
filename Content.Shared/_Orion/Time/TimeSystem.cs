namespace Content.Shared._Orion.Time;

//
// License-Identifier: AGPL-3.0-or-later
//

public sealed class TimeSystem : EntitySystem
{
    public DateTime GetStationDate()
    {
        var today = DateTime.UtcNow.Date;
        var futureYear = today.Year + 500;
        var day = Math.Min(today.Day, DateTime.DaysInMonth(futureYear, today.Month));
        var stationDate = new DateTime(futureYear, today.Month, day);

        return stationDate;
    }

    public TimeSpan GetStationTime() // TODO: Randomize this?
    {
        return DateTime.UtcNow.TimeOfDay;
    }
}
