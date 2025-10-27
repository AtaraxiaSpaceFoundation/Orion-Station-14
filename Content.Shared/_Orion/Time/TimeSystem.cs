using Content.Shared.GameTicking;
using Robust.Shared.Timing;

namespace Content.Shared._Orion.Time;

//
// License-Identifier: AGPL-3.0-or-later
//

public sealed class TimeSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    private TimeSpan _roundStart = TimeSpan.Zero;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<TickerLobbyStatusEvent>(OnLobbyStatus);
    }

    private void OnLobbyStatus(TickerLobbyStatusEvent ev)
    {
        _roundStart = ev.RoundStartTimeSpan;
    }

    public (TimeSpan Time, DateTime Date) GetStationTime()
    {
        var elapsed = _timing.CurTime - _roundStart;
        if (elapsed < TimeSpan.Zero)
            elapsed = TimeSpan.Zero;

        var timeOfDay = TimeSpan.FromTicks(elapsed.Ticks % TimeSpan.TicksPerDay);

        var today = DateTime.UtcNow.Date;
        var futureYear = today.Year + 500;
        var day = Math.Min(today.Day, DateTime.DaysInMonth(futureYear, today.Month));
        var stationDate = new DateTime(futureYear, today.Month, day);

        return (timeOfDay, stationDate);
    }
}
