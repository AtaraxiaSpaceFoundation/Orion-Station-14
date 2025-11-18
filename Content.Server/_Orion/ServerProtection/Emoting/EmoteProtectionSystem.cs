using Content.Shared.Administration.Managers;
using Content.Shared.CCVar;
using Content.Shared.Speech;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Random;

namespace Content.Server._Orion.ServerProtection.Emoting;

//
// License-Identifier: AGPL-3.0-or-later
//

public sealed class EmoteProtectionSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly ISharedAdminManager _admin = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ServerProtectionPunishmentSystem _punishment = default!;

    private ISawmill _log = default!;

    private bool _protectionEnabled;
    private bool _eraseEnabled;
    private bool _banEnabled;
    private bool _kickEnabled;
    private bool _deleteMessagesEnabled;
    private int _banDuration;

    private int _hardEmoteThreshold;
    private int _softThresholdVariance;
    private float _postSoftThresholdProbability;
    private float _softThresholdRefreshCooldown;
    private float _clearInterval;

    private int? _softThreshold;
    private readonly Dictionary<EntityUid, int> _emoteTracker = new();
    private float _timeSinceLastClear;
    private float _timeSinceLastRefresh;

    public override void Initialize()
    {
        base.Initialize();

        _log = Logger.GetSawmill("serverprotection.emote_protection");

        _cfg.OnValueChanged(CCVars.EmoteProtectionEnabled, v => _protectionEnabled = v, true);
        _cfg.OnValueChanged(CCVars.EmoteProtectionEraseEnabled, v => _eraseEnabled = v, true);
        _cfg.OnValueChanged(CCVars.EmoteProtectionBanEnabled, v => _banEnabled = v, true);
        _cfg.OnValueChanged(CCVars.EmoteProtectionKickEnabled, v => _kickEnabled = v, true);
        _cfg.OnValueChanged(CCVars.EmoteProtectionDeleteMessages, v => _deleteMessagesEnabled = v, true);
        _cfg.OnValueChanged(CCVars.EmoteProtectionBanDuration, v => _banDuration = v, true);

        _cfg.OnValueChanged(CCVars.EmoteProtectionHardThreshold, v => _hardEmoteThreshold = v, true);
        _cfg.OnValueChanged(CCVars.EmoteProtectionSoftVariance, v => _softThresholdVariance = v, true);
        _cfg.OnValueChanged(CCVars.EmoteProtectionPostSoftProbability, v => _postSoftThresholdProbability = v, true);
        _cfg.OnValueChanged(CCVars.EmoteProtectionSoftRefreshCooldown, v => _softThresholdRefreshCooldown = v, true);
        _cfg.OnValueChanged(CCVars.EmoteProtectionClearInterval, v => _clearInterval = v, true);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _timeSinceLastClear += frameTime;
        _timeSinceLastRefresh += frameTime;

        if (_timeSinceLastClear >= _clearInterval)
        {
            _emoteTracker.Clear();
            _timeSinceLastClear = 0f;
        }

        if (!(_timeSinceLastRefresh >= _softThresholdRefreshCooldown))
            return;

        GetSoftThreshold(true);
        _timeSinceLastRefresh = 0f;
    }

    public void OnEmoteDetected(EntityUid uid, string emote, bool voluntary)
    {
        if (!_protectionEnabled || !voluntary)
            return;

        if (!TryComp<SpeechComponent>(uid, out _))
            return;

        if (!_playerManager.TryGetSessionByEntity(uid, out _)) // Dont ban NPCs
            return;

        if (_admin.IsAdmin(uid, true))
            return;

        Add(uid);
    }

    private void Add(EntityUid uid)
    {
        var count = _emoteTracker.GetValueOrDefault(uid) + 1;
        _emoteTracker[uid] = count;

        TryHardThresholdViolation(uid, count);
        TrySoftThresholdViolation(uid, count);
    }

    private void TryHardThresholdViolation(EntityUid uid, int count)
    {
        if (count >= _hardEmoteThreshold)
            HandleViolation(uid, "hard", count.ToString());
    }

    private void TrySoftThresholdViolation(EntityUid uid, int count)
    {
        var soft = GetSoftThreshold();

        if (count < soft)
            return;

        var steps = count - soft;
        var chance = steps * _postSoftThresholdProbability;

        if (_random.Prob(chance))
            HandleViolation(uid, "soft", count.ToString());
    }

    private int GetSoftThreshold(bool refresh = false)
    {
        if (_softThreshold != null && !refresh)
            return _softThreshold.Value;

        var baseThreshold = _hardEmoteThreshold * 3 / 4;
        var randomReduction = _random.Next(0, _softThresholdVariance);
        _softThreshold = Math.Max(2, baseThreshold - randomReduction);

        return _softThreshold.Value;
    }

    private void HandleViolation(EntityUid uid, string emoteText, string count)
    {
        if (!_playerManager.TryGetSessionByEntity(uid, out var session))
            return;

        var word = emoteText.Length > 30
            ? emoteText[..30] + "..."
            : emoteText;
        var banReason = Loc.GetString("emote-protection-ban-reason", ("word", word), ("count", count));
        var kickReason = Loc.GetString("emote-protection-kick-reason", ("word", word), ("count", count));

        _log.Info($"{session.Name} ({session.UserId}) превысил лимит эмоций: {count} раз!");

        if (_deleteMessagesEnabled)
            _punishment.DeleteMessages(session);

        if (_eraseEnabled)
            _punishment.EraseCharacter(session);

        if (_banEnabled)
        {
            _punishment.SendAdminAlert(Loc.GetString("emote-protection-admin-announcement-ban-reason",
                ("player", session.Name),
                ("word", word),
                ("count", count)));
            _punishment.ApplyBan(session, banReason, _banDuration);
        }
        else if (_kickEnabled)
        {
            _punishment.SendAdminAlert(Loc.GetString("emote-protection-admin-announcement-kick-reason",
                ("player", session.Name),
                ("word", word),
                ("count", count)));
            _punishment.KickPlayer(session, kickReason);
        }

        _emoteTracker.Remove(uid);
    }
}
