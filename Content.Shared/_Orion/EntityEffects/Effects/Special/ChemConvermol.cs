using System.Collections.Generic;
using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Shared._Orion.EntityEffects.Effects;

/// <summary>
/// Heals Airloss damage group and deals proportional toxic byproducts
/// based on actual healing done. When not overdosed, healing is capped to
/// current damage + buffer, ensuring a minimum tox even with no airloss damage.
/// Overdose removes the cap.
/// </summary>
[UsedImplicitly]
public sealed partial class ChemConvermol : EntityEffect
{
    [DataField]
    public ProtoId<DamageGroupPrototype> HealDamageGroup = "Airloss";

    [DataField]
    public string ToxDamageType = "Poison";

    [DataField]
    public float HealPerTick = 1f;

    [DataField]
    public float Buffer = 0.5f;

    [DataField]
    public float ToxRatio = 5f;

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
        if (args is not EntityEffectReagentArgs r)
            return;

        if (!args.EntityManager.TryGetComponent<DamageableComponent>(args.TargetEntity, out var dmg))
            return;

        var prototype = IoCManager.Resolve<IPrototypeManager>();
        var groupProto = prototype.Index(HealDamageGroup);
        var damSys = args.EntityManager.System<DamageableSystem>();

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

        var potential = HealPerTick * r.Scale.Float();
        var overdosed = r.Quantity.Float() >= OverdoseThreshold;

        float actualHeal;
        if (!overdosed)
            actualHeal = Math.Max(0f, Math.Min(potential, currentDamage + Buffer));
        else
            actualHeal = potential;

        if (actualHeal > 0f && currentDamage > 0f)
        {
            var healSpec = new DamageSpecifier();
            foreach (var (typeId, damage) in damageByType)
            {
                healSpec.DamageDict[typeId] = FixedPoint2.New(-(actualHeal * damage / currentDamage));
            }
            damSys.TryChangeDamage(args.TargetEntity, healSpec, true, interruptsDoAfters: false);
        }

        var tox = actualHeal / ToxRatio;
        if (tox > 0f)
        {
            var toxSpec = new DamageSpecifier();
            toxSpec.DamageDict[ToxDamageType] = FixedPoint2.New(tox);
            damSys.TryChangeDamage(args.TargetEntity, toxSpec, true, interruptsDoAfters: false);
        }
    }
}
