using Content.Shared.CCVar;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Server._Orion.ServerProtection.Administration;

//
// License-Identifier: AGPL-3.0-or-later
//

public sealed class AdminActionProtectionSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly ServerProtectionPunishmentSystem _punishment = default!;
    [Dependency] private readonly ServerProtectionAuditManager _toggleAudit = default!;

    private ISawmill _log = default!;

    private bool _protectionEnabled;
    private int _banThreshold;
    private float _banWindowSeconds;
    private int _permissionThreshold;
    private float _permissionWindowSeconds;
    private float _alertCooldownSeconds;
    private bool _autoDeAdminEnabled;
    private bool _autoBanEnabled;
    private int _autoBanMinutes;
    private bool _initialized;

    private readonly Dictionary<(NetUserId Admin, ActionKind Kind), Queue<TimeSpan>> _actions = new();
    private readonly Dictionary<(NetUserId Admin, ActionKind Kind), TimeSpan> _lastAlert = new();

    private enum ActionKind
    {
        Ban,
        Permission
    }

    public override void Initialize()
    {
        base.Initialize();

        _log = Logger.GetSawmill("serverprotection.admin_action_protection");

        _cfg.OnValueChanged(CCVars.AdminActionProtectionEnabled, OnProtectionEnabledChanged, true);
        _cfg.OnValueChanged(CCVars.AdminActionProtectionBanThreshold, v => _banThreshold = v, true);
        _cfg.OnValueChanged(CCVars.AdminActionProtectionBanWindowSeconds, v => _banWindowSeconds = v, true);
        _cfg.OnValueChanged(CCVars.AdminActionProtectionPermissionThreshold, v => _permissionThreshold = v, true);
        _cfg.OnValueChanged(CCVars.AdminActionProtectionPermissionWindowSeconds, v => _permissionWindowSeconds = v, true);
        _cfg.OnValueChanged(CCVars.AdminActionProtectionAlertCooldownSeconds, v => _alertCooldownSeconds = v, true);
        _cfg.OnValueChanged(CCVars.AdminActionProtectionAutoDeAdminEnabled, OnAutoDeAdminChanged, true);
        _cfg.OnValueChanged(CCVars.AdminActionProtectionAutoBanEnabled, OnAutoBanChanged, true);
        _cfg.OnValueChanged(CCVars.AdminActionProtectionAutoBanMinutes, v => _autoBanMinutes = v, true);

        _initialized = true;
    }

    private void OnProtectionEnabledChanged(bool enabled)
    {
        var old = _protectionEnabled;
        _protectionEnabled = enabled;

        if (!_initialized || old == enabled)
            return;

        AnnounceToggle("AdminActionProtection", CCVars.AdminActionProtectionEnabled.Name, enabled);
    }

    private void OnAutoDeAdminChanged(bool enabled)
    {
        var old = _autoDeAdminEnabled;
        _autoDeAdminEnabled = enabled;

        if (!_initialized || old == enabled)
            return;

        AnnounceToggle("AdminActionProtection.AutoDeAdmin", CCVars.AdminActionProtectionAutoDeAdminEnabled.Name, enabled);
    }

    private void OnAutoBanChanged(bool enabled)
    {
        var old = _autoBanEnabled;
        _autoBanEnabled = enabled;

        if (!_initialized || old == enabled)
            return;

        AnnounceToggle("AdminActionProtection.AutoBan", CCVars.AdminActionProtectionAutoBanEnabled.Name, enabled);
    }

    private void AnnounceToggle(string systemName, string cvarName, bool enabled)
    {
        var actor = _toggleAudit.TryGetRecentActor(cvarName, TimeSpan.FromSeconds(5), out var knownActor)
            ? knownActor
            : "unknown";

        var state = enabled ? "включена" : "ВЫКЛЮЧЕНА";
        var message = $"[ServerProtection] Система {systemName} была {state}. Переключил: {actor}.";
        _punishment.SendAdminAlert(message);
        _log.Info(message);
    }

    public void ReportBanAction(NetUserId adminUserId, string adminName, string targetName)
    {
        if (!_protectionEnabled)
            return;

        if (!RegisterAction(adminUserId, ActionKind.Ban, _banThreshold, _banWindowSeconds))
            return;

        var message = $"[ServerProtection] Возможный саботаж: администратор {adminName} ({adminUserId}) выдал слишком много банов за короткое время. Последняя цель: {targetName}.";
        _punishment.SendAdminAlert(message);
        _log.Warning(message);

        EnforceMeasures(adminUserId, adminName, "mass-ban-detected");
    }

    public void ReportPermissionRemovalAction(NetUserId adminUserId, string adminName, string targetName, string action)
    {
        if (!_protectionEnabled)
            return;

        if (!RegisterAction(adminUserId, ActionKind.Permission, _permissionThreshold, _permissionWindowSeconds))
            return;

        var message = $"[ServerProtection] Возможный саботаж: администратор {adminName} ({adminUserId}) массово снимает права/статус у администраторов. Последнее действие: {action} для {targetName}.";
        _punishment.SendAdminAlert(message);
        _log.Warning(message);

        EnforceMeasures(adminUserId, adminName, "permission-stripping-detected");
    }

    private void EnforceMeasures(NetUserId adminUserId, string adminName, string reasonTag)
    {
        if (_autoDeAdminEnabled && _player.TryGetSessionById(adminUserId, out var adminSession))
        {
            _punishment.DeAdmin(adminSession, $"AdminActionProtection: {reasonTag}");
            var deAdminMessage = $"[ServerProtection] Автоматическая мера: {adminName} ({adminUserId}) был deadmin из-за подозрения на саботаж ({reasonTag}).";
            _punishment.SendAdminAlert(deAdminMessage);
            _log.Warning(deAdminMessage);
        }

        if (_autoBanEnabled)
        {
            var reason = $"ServerProtection auto-ban: suspicious admin actions ({reasonTag}).";
            _punishment.ApplyBan(adminUserId, adminName, reason, _autoBanMinutes);
            var banMessage = $"[ServerProtection] Автоматическая мера: для {adminName} ({adminUserId}) выдан бан на {_autoBanMinutes} мин. Причина: {reasonTag}.";
            _punishment.SendAdminAlert(banMessage);
            _log.Warning(banMessage);
        }
    }

    private bool RegisterAction(NetUserId adminUserId, ActionKind actionKind, int threshold, float windowSeconds)
    {
        var key = (adminUserId, actionKind);

        if (!_actions.TryGetValue(key, out var queue))
        {
            queue = new Queue<TimeSpan>();
            _actions[key] = queue;
        }

        var now = _timing.CurTime;
        var window = TimeSpan.FromSeconds(Math.Max(1f, windowSeconds));

        queue.Enqueue(now);

        while (queue.Count > 0 && now - queue.Peek() > window)
        {
            queue.Dequeue();
        }

        if (queue.Count < Math.Max(1, threshold))
            return false;

        if (_lastAlert.TryGetValue(key, out var lastAlertAt) && now - lastAlertAt < TimeSpan.FromSeconds(Math.Max(1f, _alertCooldownSeconds)))
            return false;

        _lastAlert[key] = now;
        return true;
    }
}
