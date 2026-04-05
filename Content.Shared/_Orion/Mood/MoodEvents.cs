using Content.Shared.Alert;
using Robust.Shared.Serialization;

namespace Content.Shared._Orion.Mood;

[Serializable, NetSerializable]
public sealed class MoodEffectEvent : EntityEventArgs
{
    /// <summary>
    ///     ID of the moodlet prototype to use
    /// </summary>
    public string EffectId;

    /// <summary>
    ///     How much should the mood change be multiplied by
    ///     <br />
    ///     This does nothing if the moodlet ID matches one with the same Category
    /// </summary>
    public float EffectModifier;

    /// <summary>
    ///     How much should the mood change be offset by, after multiplication
    ///     <br />
    ///     This does nothing if the moodlet ID matches one with the same Category
    /// </summary>
    public float EffectOffset;

    public MoodEffectEvent(string effectId, float effectModifier = 1f, float effectOffset = 0f)
    {
        EffectId = effectId;
        EffectModifier = effectModifier;
        EffectOffset = effectOffset;
    }
}

[Serializable, NetSerializable]
public sealed class MoodRemoveEffectEvent : EntityEventArgs
{
    public string EffectId;
    public MoodEffectRemovalReason Reason;

    public MoodRemoveEffectEvent(string effectId, MoodEffectRemovalReason reason = MoodEffectRemovalReason.Manual)
    {
        EffectId = effectId;
        Reason = reason;
    }
}

[Serializable, NetSerializable]
public enum MoodEffectRemovalReason : byte
{
    Manual = 0,
    Expired = 1,
}

[Serializable, NetSerializable]
public sealed class MoodPurgeEffectsEvent : EntityEventArgs
{
    public bool RemovePermanentMoodlets;

    public MoodPurgeEffectsEvent(bool removePermanentMoodlets)
    {
        RemovePermanentMoodlets = removePermanentMoodlets;
    }
}

/// <summary>
///     This event is raised whenever an entity sets their mood, allowing other systems to modify the end result of mood math.
///     EG: The end result after tallying up all Moodlets comes out to 70, but a trait multiplies it by 0.8 to make it 56.
/// </summary>
[ByRefEvent]
public record struct OnSetMoodEvent(EntityUid Receiver, float MoodChangedAmount, bool Cancelled, float MoodOffset = 0f);

/// <summary>
///     This event is raised on an entity when it receives a mood effect, but before the effects are calculated.
///     Allows for other systems to pick and choose specific events to modify.
/// </summary>
[ByRefEvent]
public record struct OnMoodEffect(EntityUid Receiver, string EffectId, float EffectModifier = 1f, float EffectOffset = 0f);

public sealed partial class ShowMoodAlertEvent : BaseAlertEvent;
