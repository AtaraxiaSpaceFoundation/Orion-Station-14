using Content.Goobstation.Maths.FixedPoint;
using Content.Shared._Orion.Mood;
using Content.Shared.Alert;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;

namespace Content.Server._Orion.Mood;

[RegisterComponent]
public sealed partial class MoodComponent : Component
{
    [DataField]
    public float CurrentMood;

    [DataField]
    public float CurrentShownMood;

    [DataField]
    public float CurrentMoodLevel;

    [DataField]
    public MoodThreshold CurrentMoodThreshold;

    [DataField]
    public MoodThreshold LastThreshold;

    [DataField]
    public float CurrentSanity = 100f;

    [DataField]
    public float MinSanity;

    [DataField]
    public float MaxSanity = 150f;

    [DataField]
    public float SanityRecoveryRate = 42f;

    [DataField]
    public float UnstableFloorSanity = 50f;

    [DataField]
    public SanityThreshold CurrentSanityThreshold = SanityThreshold.Disturbed;

    [DataField]
    public SanityThreshold LastSanityThreshold = SanityThreshold.Disturbed;

    public readonly Dictionary<string, string> CategorisedEffects = new();

    public readonly Dictionary<string, int> CategorisedEffectTimerGenerations = new();

    public readonly Dictionary<string, float> UncategorisedEffects = new();

    public readonly Dictionary<string, int> UncategorisedEffectTimerGenerations = new();

    /// <summary>
    ///     The formula for the movement speed modifier is SpeedBonusGrowth ^ (MoodLevel - MoodThreshold.Neutral).
    ///     Change this ONLY BY 0.001 AT A TIME.
    /// </summary>
    [DataField]
    public float SpeedBonusGrowth = 1.003f;

    /// <summary>
    ///     The lowest point that low morale can multiply our movement speed by. Lowering speed follows a linear curve, rather than geometric.
    /// </summary>
    [DataField]
    public float MinimumSpeedModifier = 0.75f;

    /// <summary>
    ///     The maximum amount that high morale can multiply our movement speed by. This follows a significantly slower geometric sequence.
    /// </summary>
    [DataField]
    public float MaximumSpeedModifier = 1.15f;

    [DataField]
    public float IncreaseCritThreshold = 1.2f;

    [DataField]
    public float DecreaseCritThreshold = 0.9f;

    public FixedPoint2 SoftCritThresholdBeforeModify;
    public FixedPoint2 HardCritThresholdBeforeModify;
    public FixedPoint2 DeadThresholdBeforeModify;

    [DataField]
    public ProtoId<AlertCategoryPrototype> MoodCategory = "Mood";

    [DataField(customTypeSerializer: typeof(DictionarySerializer<MoodThreshold, float>))]
    public Dictionary<MoodThreshold, float> MoodThresholds = new()
    {
        { MoodThreshold.Insane, 120f },
        { MoodThreshold.Perfect, 100f },
        { MoodThreshold.Exceptional, 80f },
        { MoodThreshold.Great, 70f },
        { MoodThreshold.Good, 60f },
        { MoodThreshold.Neutral, 50f },
        { MoodThreshold.Meh, 40f },
        { MoodThreshold.Bad, 30f },
        { MoodThreshold.Terrible, 20f },
        { MoodThreshold.Horrible, 10f },
        { MoodThreshold.Dead, 0f },
    };

    [DataField(customTypeSerializer: typeof(DictionarySerializer<MoodThreshold, ProtoId<AlertPrototype>>))]
    public Dictionary<MoodThreshold, ProtoId<AlertPrototype>> MoodThresholdsAlerts = new()
    {
        { MoodThreshold.Dead, "MoodDead" },
        { MoodThreshold.Horrible, "Horrible" },
        { MoodThreshold.Terrible, "Terrible" },
        { MoodThreshold.Bad, "Bad" },
        { MoodThreshold.Meh, "Meh" },
        { MoodThreshold.Neutral, "Neutral" },
        { MoodThreshold.Good, "Good" },
        { MoodThreshold.Great, "Great" },
        { MoodThreshold.Exceptional, "Exceptional" },
        { MoodThreshold.Perfect, "Perfect" },
        { MoodThreshold.Insane, "Insane" },
    };

    [DataField(customTypeSerializer: typeof(DictionarySerializer<SanityThreshold, float>))]
    public Dictionary<SanityThreshold, float> SanityThresholds = new()
    {
        { SanityThreshold.Great, 125f },
        { SanityThreshold.Disturbed, 100f },
        { SanityThreshold.Unstable, 75f },
        { SanityThreshold.Crazy, 50f },
        { SanityThreshold.Insane, 25f },
    };

    [DataField(customTypeSerializer: typeof(DictionarySerializer<MoodThreshold, float>))]
    public Dictionary<MoodThreshold, float> SanityDeltaPerSecond = new()
    {
        { MoodThreshold.Insane, -0.30f },
        { MoodThreshold.Perfect, 0.60f },
        { MoodThreshold.Exceptional, 0.40f },
        { MoodThreshold.Great, 0.30f },
        { MoodThreshold.Good, 0.20f },
        { MoodThreshold.Neutral, 0f },
        { MoodThreshold.Meh, -0.05f },
        { MoodThreshold.Bad, -0.10f },
        { MoodThreshold.Terrible, -0.15f },
        { MoodThreshold.Horrible, -0.30f },
        { MoodThreshold.Dead, 0f },
    };

    /// <summary>
    ///     These thresholds represent a percentage of Crit-Threshold, 0.8 corresponding with 80%.
    /// </summary>
    [DataField(customTypeSerializer: typeof(DictionarySerializer<ProtoId<MoodEffectPrototype>, float>))]
    public Dictionary<ProtoId<MoodEffectPrototype>, float> HealthMoodEffectsThresholds = new()
    {
        { "HealthHeavyDamage", 0.8f },
        { "HealthSevereDamage", 0.5f },
        { "HealthLightDamage", 0.1f },
        { "HealthNoDamage", 0.05f },
    };
}

[Serializable]
public enum MoodThreshold : ushort
{
    Dead = 0,
    Horrible = 1,
    Terrible = 2,
    Bad = 3,
    Meh = 4,
    Neutral = 5,
    Good = 6,
    Great = 7,
    Exceptional = 8,
    Perfect = 9,
    Insane = 10,
}

[Serializable]
public enum SanityThreshold : ushort
{
    Insane = 0,
    Crazy = 1,
    Unstable = 2,
    Disturbed = 3,
    Great = 4,
}
