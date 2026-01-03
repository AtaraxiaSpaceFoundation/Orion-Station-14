using System.Linq;
using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.Preferences.Managers;
using Content.Shared._Orion.ReadyManifest;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Orion.ReadyManifest;

public sealed class ReadyManifestSystem : EntitySystem
{
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IServerPreferencesManager _prefsManager = default!;

    private readonly Dictionary<ICommonSession, ReadyManifestEui> _openEuis = new();
    private Dictionary<ProtoId<JobPrototype>, int> _jobCounts = new();

    public override void Initialize()
    {
        SubscribeNetworkEvent<RequestReadyManifestMessage>(OnRequestReadyManifest);
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
        SubscribeLocalEvent<PlayerToggleReadyEvent>(OnPlayerToggleReady);
    }

    private void OnRoundStarting(RoundStartingEvent ev)
    {
        foreach (var (_, eui) in _openEuis)
        {
            eui.Close();
        }

        _openEuis.Clear();
    }

    private void OnRequestReadyManifest(RequestReadyManifestMessage message, EntitySessionEventArgs args)
    {
        if (args.SenderSession is not { } sessionCast
            || !_configManager.GetCVar(CCVars.CrewManifestWithoutEntity))
            return;

        BuildReadyManifest();
        OpenEui(sessionCast, args.SenderSession.AttachedEntity);
    }

    private void OnPlayerToggleReady(PlayerToggleReadyEvent ev)
    {
        var userId = ev.PlayerSession.Data.UserId;

        if (!_prefsManager.TryGetCachedPreferences(userId, out var preferences))
            return;

        var profile = (HumanoidCharacterProfile) preferences.SelectedCharacter;
        var profileJobs = FilterPlayerJobs(profile);

        if (_gameTicker.PlayerGameStatuses[userId] == PlayerGameStatus.ReadyToPlay)
        {
            foreach (var job in profileJobs)
            {
                if (!_jobCounts.TryAdd(job, 1))
                {
                    _jobCounts[job]++;
                }
            }
        }
        else
        {
            foreach (var job in profileJobs)
            {
                if (_jobCounts.TryGetValue(job, out var value))
                {
                    _jobCounts[job] = --value;
                }
            }
        }

        UpdateEuis();
    }

    private void BuildReadyManifest()
    {
        var jobCounts = new Dictionary<ProtoId<JobPrototype>, int>();

        foreach (var (userId, status) in _gameTicker.PlayerGameStatuses)
        {
            if (status != PlayerGameStatus.ReadyToPlay)
                continue;

            if (!_prefsManager.TryGetCachedPreferences(userId, out var preferences))
                continue;

            var profile = (HumanoidCharacterProfile) preferences.SelectedCharacter;
            var profileJobs = FilterPlayerJobs(profile);
            foreach (var jobId in profileJobs)
            {
                if (!jobCounts.TryAdd(jobId, 1))
                    jobCounts[jobId]++;
            }
        }
        _jobCounts = jobCounts;
    }

    private List<ProtoId<JobPrototype>> FilterPlayerJobs(HumanoidCharacterProfile profile)
    {
        var jobs = profile.JobPriorities.Keys.Select(k => new ProtoId<JobPrototype>(k)).ToList();
        List<ProtoId<JobPrototype>> priorityJobs = [];
        foreach (var job in jobs)
        {
            var priority = profile.JobPriorities[job];
            if (priority == JobPriority.High || (_prototypeManager.Index(job).Weight >= 10 && priority > JobPriority.Never))
                priorityJobs.Add(job);
        }

        return priorityJobs;
    }

    public Dictionary<ProtoId<JobPrototype>, int> GetReadyManifest()
    {
        return _jobCounts;
    }

    public void OpenEui(ICommonSession session, EntityUid? owner = null)
    {
        if (_openEuis.ContainsKey(session))
            return;

        var eui = new ReadyManifestEui(owner, this);
        _openEuis.Add(session, eui);
        _euiManager.OpenEui(eui, session);
        eui.StateDirty();
    }

    private void UpdateEuis()
    {
        foreach (var (_, eui) in _openEuis)
        {
            eui.StateDirty();
        }
    }

    /// <summary>
    ///     Closes an EUI for a given player.
    /// </summary>
    /// <param name="session">The player's session.</param>
    /// <param name="owner">The owner of this EUI, if there was one.</param>
    public void CloseEui(ICommonSession session, EntityUid? owner = null)
    {
        if (!_openEuis.TryGetValue(session, out var eui))
        {
            return;
        }

        if (eui.Owner != owner)
            return;

        _openEuis.Remove(session);
        eui.Close();
    }
}
