using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared._Orion.EntityEffects.Effects;

/// <summary>
/// Heals asphyxiation damage and deals proportional toxic byproducts
/// based on actual healing done. When not overdosed, healing is capped to
/// current damage + buffer, ensuring a minimum ~tox even with no oxygen damage.
/// Overdose removes the cap.
/// </summary>
[UsedImplicitly]
public sealed partial class ChemConvermol : EntityEffect
{
    /// <summary>
    /// Damage type to heal (default: Asphyxiation).
    /// </summary>
    [DataField]
    public string HealDamageType = "Asphyxiation";

    /// <summary>
    /// Damage type for toxic byproduct (default: Poison).
    /// </summary>
    [DataField]
    public string ToxDamageType = "Poison";

    /// <summary>
    /// Flat heal per tick (scaled by reagent purity via Scale).
    /// SS13 equivalent: ~1 oxy/tick at base metabolization_ratio 0.2.
    /// </summary>
    [DataField]
    public float HealPerTick = 1f;

    /// <summary>
    /// Buffer added on top of current damage when capping heal.
    /// At 0.5 this ensures minimum ~0.1 tox even with no oxygen damage.
    /// </summary>
    [DataField]
    public float Buffer = 0.5f;

    /// <summary>
    /// actualHeal / ToxRatio = tox damage dealt per tick.
    /// </summary>
    [DataField]
    public float ToxRatio = 5f;

    /// <summary>
    /// Reagent quantity at which overdose behaviour activates.
    /// </summary>
    [DataField]
    public float OverdoseThreshold = 35f;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-convermol",
            ("rate", HealPerTick),
            ("ratio", ToxRatio),
            ("od", OverdoseThreshold));

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (args is not EntityEffectReagentArgs r)
            return;

        if (!args.EntityManager.TryGetComponent<DamageableComponent>(args.TargetEntity, out var dmg))
            return;

        var damSys = args.EntityManager.System<DamageableSystem>();
        var quantity = (float) r.Quantity;
        var overdosed = quantity >= OverdoseThreshold;

        // Flat rate × purity — no quantity multiplication (SS14 HealthChange pattern)
        var potential = HealPerTick * (float) r.Scale;

        float actualHeal;
        if (!overdosed)
        {
            var current = dmg.Damage.DamageDict.TryGetValue(HealDamageType, out var v) ? v.Float() : 0f;
            // Buffer=0.5 → minimum ~0.1 tox at 0 oxy damage (mirrors SS13 behaviour)
            actualHeal = Math.Max(0f, Math.Min(potential, current + Buffer));
        }
        else
        {
            actualHeal = potential;
        }

        if (actualHeal > 0f)
        {
            var healSpec = new DamageSpecifier
            {
                DamageDict = new Dictionary<string, FixedPoint2> { { HealDamageType, -actualHeal } }
            };
            damSys.TryChangeDamage(args.TargetEntity, healSpec, true, interruptsDoAfters: false);
        }

        // Tox proportional to ACTUAL heal — coupling handled here
        var tox = actualHeal / ToxRatio;
        if (tox > 0f)
        {
            var toxSpec = new DamageSpecifier
            {
                DamageDict = new Dictionary<string, FixedPoint2> { { ToxDamageType, tox } }
            };
            damSys.TryChangeDamage(args.TargetEntity, toxSpec, true, interruptsDoAfters: false);
        }
    }
}
