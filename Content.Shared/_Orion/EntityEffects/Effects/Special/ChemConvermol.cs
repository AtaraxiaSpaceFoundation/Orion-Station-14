using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
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
    public ProtoId<DamageGroupPrototype> HealDamageGroup = "Airloss";

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
            ("chance", Probability),
            ("rate", HealPerTick),
            ("ratio", ToxRatio),
            ("od", OverdoseThreshold));

    public override void Effect(EntityEffectBaseArgs args)
    {
        var groupProto = args.EntityManager.System<IPrototypeManager>();
        var prototype = IoCManager.Resolve<IPrototypeManager>();
        var groupProto = prototype.Index(HealDamageGroup);

        float currentDamage = 0f;
        var damageByType = new Dictionary<string, float>();

        foreach (var damageTypeId in groupProto.DamageTypes)
        {
            if (!dmg.Damage.DamageDict.TryGetValue(damageTypeId, out var v))
                continue;
            var val = v.Float();
            if (val <= 0f)
                continue;
            damageByType[damageTypeId] = val;
            currentDamage += val;
        }

        float actualHeal;
        if (!overdosed)
        {
            actualHeal = Math.Max(0f, Math.Min(potential, currentDamage + Buffer));
        }
        else
        {
            actualHeal = potential;
        }

        // Применяем лечение пропорционально (только если есть что лечить)
        if (actualHeal > 0f && currentDamage > 0f)
        {
            var healSpec = new DamageSpecifier();
            foreach (var (typeId, damage) in damageByType)
            {
                healSpec.DamageDict[typeId] = -(FixedPoint2)(actualHeal * damage / currentDamage);
            }
            damSys.TryChangeDamage(args.TargetEntity, healSpec, true, interruptsDoAfters: false);
        }

        // Токсин — один раз от actualHeal, не меняется
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
