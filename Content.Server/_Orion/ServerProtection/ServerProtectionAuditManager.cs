using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._Orion.ServerProtection;

//
// License-Identifier: AGPL-3.0-or-later
//

public sealed class ServerProtectionAuditManager
{
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly Dictionary<string, (string Actor, TimeSpan ChangedAt)> _changes = new();

    public void RecordChange(string cvarName, ICommonSession? actor, object? oldValue, object? newValue)
    {
        if (Equals(oldValue, newValue))
            return;

        var actorInfo = actor == null
            ? "unknown"
            : $"{actor.Name} ({actor.UserId})";

        _changes[cvarName] = (actorInfo, _timing.CurTime);
    }

    public bool TryGetRecentActor(string cvarName, TimeSpan maxAge, out string actor)
    {
        if (_changes.TryGetValue(cvarName, out var change) && _timing.CurTime - change.ChangedAt <= maxAge)
        {
            actor = change.Actor;
            return true;
        }

        actor = "unknown";
        return false;
    }
}
