// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 Ilya246 <ilyukarno@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Threading.Tasks;
using Content.Server.Database;
using Robust.Shared.Asynchronous;
using Robust.Shared.Network;

// has to be in Content.Server to exist
namespace Content.Server._Goobstation.Antag
{
    public sealed class LastRolledAntagManager
    {
        [Dependency] private readonly IServerDbManager _db = default!;
        [Dependency] private readonly ITaskManager _task = default!;
        private readonly HashSet<Task> _pendingSaveTasks = new(); // Orion-Edit
        // Orion-Start
        private readonly Dictionary<NetUserId, TimeSpan> _lastRollCache = new();
        private readonly object _pendingSaveLock = new();
        private readonly object _cacheLock = new();
        // Orion-End
        private ISawmill _sawmill = default!;

        public void Initialize()
        {
            _sawmill = Logger.GetSawmill("last_antag");
        }

        /// <summary>
        /// Saves last rolled values to the database before allowing the server to shutdown.
        /// </summary>
        public void Shutdown()
        {
            // Orion-Start
            Task[] pendingTasks;
            lock (_pendingSaveLock)
            {
                pendingTasks = _pendingSaveTasks.ToArray();
            }

            _task.BlockWaitOnTask(Task.WhenAll(pendingTasks));
            // Orion-End
        }

        /// <summary>
        /// Sets a player's last rolled antag time.
        /// </summary>
        public TimeSpan SetLastRolled(NetUserId userId, TimeSpan to)
        {
            // Orion-Edit-Start
            var oldTime = GetLastRolled(userId);
            lock (_cacheLock)
            {
                _lastRollCache[userId] = to;
            }

            var setTimeTask = _db.SetLastRolledAntag(userId, to);
            _ = TrackPendingAsync(setTimeTask, userId, oldTime, to);

            _sawmill.Info($"Setting {userId} last rolled antag to {to} from {oldTime}");
            return oldTime;
            // Orion-Edit-End
        }

        /// <summary>
        /// Gets a player's last rolled antag time.
        /// </summary>
        public TimeSpan GetLastRolled(NetUserId userId)
        // Orion-Start
        {
            lock (_cacheLock)
            {
                if (_lastRollCache.TryGetValue(userId, out var cached))
                    return cached;
            }

            var rolled = _db.GetLastRolledAntag(userId).GetAwaiter().GetResult();
            lock (_cacheLock)
            {
                _lastRollCache[userId] = rolled;
            }
            return rolled;
        }
        // Orion-End

/* // Orion-Edit
            return Task.Run(() => GetTimeAsync(userId)).GetAwaiter().GetResult();
        }

        #region Internal/Async tasks

        /// <summary>
        /// Sets a player's last rolled antag time.
        /// </summary>
        private async Task SetTimeAsyncInternal(NetUserId userId, TimeSpan time, TimeSpan oldTime)
        {
            Task<bool> setTimeTask = _db.SetLastRolledAntag(userId, time);
            TrackPending(setTimeTask); // Track the Task<bool>
            bool success = await setTimeTask;

            if (success)
                _sawmill.Debug($"Successfully set LastRolledAntag for {userId} from {oldTime} to {time}");
            else
                _sawmill.Debug($"Failed to set LastRolledAntag for {userId}. Player not found or other issue.");
        }

        /// <summary>
        /// Sets a player's last rolled antag time.
        /// </summary>
        private async Task<TimeSpan> SetTimeAsync(NetUserId userId, TimeSpan to)
        {
            var oldTime = GetLastRolled(userId);
            await SetTimeAsyncInternal(userId, to, oldTime);
            return oldTime;
        }

        /// <summary>
        /// Gets a player's last rolled antag time.
        /// </summary>
        private async Task<TimeSpan> GetTimeAsync(NetUserId userId) => await _db.GetLastRolledAntag(userId);
*/

        // Orion
        #region Internal/Async tasks

        /// <summary>
        /// Track a database save task to make sure we block server shutdown on it.
        /// </summary>
        // Orion-Edit-Start
        private async Task TrackPendingAsync(Task<bool> task, NetUserId userId, TimeSpan oldTime, TimeSpan newTime)
        {
            lock (_pendingSaveLock)
            {
                _pendingSaveTasks.Add(task);
            }

            try
            {
                var success = await task;
                _sawmill.Debug(success
                    ? $"Successfully set LastRolledAntag for {userId} from {oldTime} to {newTime}"
                    : $"Failed to set LastRolledAntag for {userId}. Player not found or other issue.");
            }
            catch (Exception e)
            {
                lock (_cacheLock)
                {
                    _lastRollCache[userId] = oldTime;
                }
                _sawmill.Error($"Error while saving LastRolledAntag for {userId}: {e}");
            }
            finally
            {
                lock (_pendingSaveLock)
                {
                    _pendingSaveTasks.Remove(task);
                }
            }
        }
        // Orion-Edit-End

        #endregion
    }
}
