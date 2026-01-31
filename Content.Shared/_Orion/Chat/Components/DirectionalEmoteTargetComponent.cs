using Robust.Shared.Serialization;

namespace Content.Shared._Orion.Chat.Components;

[RegisterComponent]
public sealed partial class DirectionalEmoteTargetComponent : Component
{
    public TimeSpan LastSend = TimeSpan.MinValue;
    public TimeSpan Cooldown = TimeSpan.FromSeconds(1);
}

[Serializable, NetSerializable]
public sealed class SendDirectionalEmoteEvent : EntityEventArgs
{
    public readonly NetEntity Source;
    public readonly NetEntity Target;
    public readonly string Text;

    public SendDirectionalEmoteEvent(NetEntity source, NetEntity target, string text)
    {
        Source = source;
        Target = target;
        Text = text;
    }
}
