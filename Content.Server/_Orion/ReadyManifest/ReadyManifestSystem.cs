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
    private Dictionary<ProtoId<JobPrototype>, List<string>> _jobCharacters = new();

    private const int MinJobWeightForAutoInclude = 10;

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
        _jobCharacters.Clear();
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
        var characterName = profile.Name;

        if (_gameTicker.PlayerGameStatuses[userId] == PlayerGameStatus.ReadyToPlay)
        {
            foreach (var job in profileJobs)
            {
                if (!_jobCharacters.ContainsKey(job))
                {
                    _jobCharacters[job] = new List<string>();
                }

                if (!_jobCharacters[job].Contains(characterName))
                {
                    _jobCharacters[job].Add(characterName);
                }
            }
        }
        else
        {
            foreach (var job in profileJobs)
            {
                if (!_jobCharacters.TryGetValue(job, out var characters))
                    continue;

                characters.Remove(characterName);
                if (characters.Count == 0)
                {
                    _jobCharacters.Remove(job);
                }
            }
        }

        UpdateEuis();
    }

    private void BuildReadyManifest()
    {
        var jobCharacters = new Dictionary<ProtoId<JobPrototype>, List<string>>();

        foreach (var (userId, status) in _gameTicker.PlayerGameStatuses)
        {
            if (status != PlayerGameStatus.ReadyToPlay)
                continue;

            if (!_prefsManager.TryGetCachedPreferences(userId, out var preferences))
                continue;

            if (preferences.SelectedCharacter is not HumanoidCharacterProfile profile)
                continue;

            var characterName = profile.Name;
            var profileJobs = FilterPlayerJobs(profile);

            foreach (var jobId in profileJobs)
            {
                if (!jobCharacters.ContainsKey(jobId))
                {
                    jobCharacters[jobId] = new List<string>();
                }

                if (!jobCharacters[jobId].Contains(characterName))
                {
                    jobCharacters[jobId].Add(characterName);
                }
            }
        }

        _jobCharacters = jobCharacters;
    }

    private List<ProtoId<JobPrototype>> FilterPlayerJobs(HumanoidCharacterProfile profile)
    {
        var jobs = profile.JobPriorities.Keys.Select(k => new ProtoId<JobPrototype>(k)).ToList();
        List<ProtoId<JobPrototype>> priorityJobs = [];
        foreach (var job in jobs)
        {
            var priority = profile.JobPriorities[job.Id];
            if (priority == JobPriority.High || (_prototypeManager.Index(job).Weight >= MinJobWeightForAutoInclude && priority > JobPriority.Never))
                priorityJobs.Add(job);
        }

        return priorityJobs;
    }

    public IReadOnlyDictionary<ProtoId<JobPrototype>, List<string>> GetReadyManifest()
    {
        return _jobCharacters;
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
    }
}
