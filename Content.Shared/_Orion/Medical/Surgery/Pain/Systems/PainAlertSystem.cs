using Content.Goobstation.Maths.FixedPoint;
using Content.Shared._Shitmed.Medical.Surgery.Pain.Components;
using Content.Shared._Shitmed.Medical.Surgery.Wounds.Components;
using Content.Shared.Alert;
using Content.Shared.Body.Part;
using Content.Shared.Damage;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._Orion.Medical.Surgery.Pain.Systems;

//
// License-Identifier: AGPL-3.0-or-later
//

/// <summary>
///     System that handles showing a pain level alert to the player
/// </summary>
public sealed class PainAlertSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    private static readonly ProtoId<AlertPrototype>[] PainAlerts = ["Pain0", "Pain1", "Pain2", "Pain3"];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NerveComponent, ComponentInit>(OnNerveSystemMapInit);
        SubscribeLocalEvent<NerveComponent, DamageChangedEvent>(OnDamageChanged);
    }

    private void OnNerveSystemMapInit(EntityUid uid, NerveComponent component, ComponentInit args)
    {
        EntityUid mobUid;
        if (TryComp<BodyPartComponent>(uid, out var bodyPart) && bodyPart.Body is { } bodyUid)
            mobUid = bodyUid;
        else
            mobUid = uid;

        if (!HasComp<AlertsComponent>(mobUid))
            return;

        // Clear all pain alerts when the component initializes
        foreach (var alertId in PainAlerts)
        {
            if (_prototypeManager.TryIndex(alertId, out var alert))
                _alerts.ClearAlert(mobUid, alert);
        }
    }

    private void OnDamageChanged(EntityUid uid, NerveComponent nerve, ref DamageChangedEvent args)
    {
        // Update on both damage and healing
        if (args.DamageDelta != null) // This will be non-null for both damage and healing
            UpdatePainAlert(uid, nerve);
    }

    private void UpdatePainAlert(EntityUid uid, NerveComponent? nerve = null)
    {
        if (!Resolve(uid, ref nerve, false) || !TryComp<WoundableComponent>(uid, out var woundable))
            return;

        // Find the parent mob that should have the AlertsComponent
        EntityUid mobUid;
        if (TryComp<BodyPartComponent>(uid, out var bodyPart) && bodyPart.Body is { } bodyUid)
            mobUid = bodyUid;
        else
            mobUid = uid;

        if (!HasComp<AlertsComponent>(mobUid))
            return;

        // Check if the mob is in a critical state
        var isCritical = false;
        if (TryComp<MobStateComponent>(mobUid, out var mobState))
            isCritical = _mobState.IsCritical(mobUid, mobState);

        var totalPain = 0f;

        if (nerve.PainFeels > 0 && woundable.IntegrityCap > 0 && woundable.WoundableIntegrity < woundable.IntegrityCap)
        {
            var normalizedIntegrity = woundable.WoundableIntegrity / woundable.IntegrityCap;
            var painLevel = (FixedPoint2.New(1) - normalizedIntegrity) * 100 * nerve.PainFeels;
            totalPain = (float) FixedPoint2.Clamp(painLevel, 0, 100);
        }

        // Always show alert if in critical state, otherwise follow normal pain rules
        if (isCritical || totalPain > 1f)
        {
            var alertIndex = isCritical
                ? 3 // Max severity in critical state
                : (int) Math.Clamp(Math.Floor((totalPain - 1f) / 25f), 0, 3);

            _alerts.ShowAlert(mobUid, PainAlerts[alertIndex]);
        }
        else
        {
            _alerts.ClearAlertCategory(mobUid, "Pain");
        }
    }
}
