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
        var elapsed = _timing.CurTime.Subtract(_roundStart);
        var totalHours = elapsed.TotalHours;

        var days = (int)(totalHours / 24);
        var remainingHours = totalHours % 24;

        var startDate = DateTime.UtcNow.Date;
        var futureYear = startDate.Year + 500;
        var stationDate = new DateTime(futureYear, startDate.Month, startDate.Day).AddDays(days);

        return (TimeSpan.FromHours(remainingHours), stationDate);
    }
}
