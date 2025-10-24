using System.Linq;
using Content.Server.AlertLevel;
using Content.Server.Station.Systems;
using Content.Shared._Orion.AlertLevelLock.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Robust.Shared.Prototypes;

namespace Content.Server._Orion.AlertLevelLock;

//
// License-Identifier: AGPL-3.0-or-later
//

public sealed class StationAlertLevelLockSystem : EntitySystem
{
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly AlertLevelSystem _level = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationAlertLevelLockComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<StationAlertLevelLockComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<StationAlertLevelLockComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<AlertLevelChangedEvent>(OnAlertChanged);
    }

    public void OnInit(EntityUid uid, StationAlertLevelLockComponent component, MapInitEvent args)
    {
        var station = _station.GetOwningStation(uid);

        if (station == null)
        {
            component.Enabled = false;
            Dirty(uid, component);
            return;
        }

        component.StationId = station.Value;
        UpdateLockState(component, _level.GetLevel(component.StationId.Value));
        Dirty(uid, component);
    }

    private void OnAlertChanged(AlertLevelChangedEvent args)
    {
        var enumerator = _entMan.AllEntityQueryEnumerator<StationAlertLevelLockComponent>();
        while (enumerator.MoveNext(out var uid, out var component))
        {
            var station = args.Station;

            if (component.StationId != station)
                continue;

            UpdateLockState(component, args.AlertLevel);
            Dirty(uid, component);
        }
    }

    private void UpdateLockState(StationAlertLevelLockComponent component, string newAlertLevel)
    {
        component.Locked = component.LockedAlertLevels.Contains(newAlertLevel);
    }

    private void OnEmagged(EntityUid uid, StationAlertLevelLockComponent component, ref GotEmaggedEvent args)
    {
        args.Handled = true;
        component.Enabled = false;
        Dirty(uid, component);
    }

    public void OnExamined(EntityUid uid, StationAlertLevelLockComponent component, ExaminedEvent args)
    {
        if (!component.Enabled || component.LockedAlertLevels.Count == 0)
            return;

        var levels = string.Join(", ",
            component.LockedAlertLevels.Select(level =>
            {
                var color = GetAlertColor(level);
                return $"[color={color.ToHex()}]{Loc.GetString($"alert-level-{level}").ToLower()}[/color]";
            }));

        args.PushMarkup(Loc.GetString("station-alert-level-lock-examined", ("levels", levels)));
    }

    private Color GetAlertColor(string alertLevel)
    {
        const string prototypeId = "stationAlerts";

        if (!_prototypeManager.TryIndex<AlertLevelPrototype>(prototypeId, out var alertSet))
            return Color.White;

        if (!alertSet.Levels.TryGetValue(alertLevel, out var levelData))
            return Color.White;

        return levelData.Color;
    }
}
