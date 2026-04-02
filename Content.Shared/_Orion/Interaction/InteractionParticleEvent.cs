using Robust.Shared.Serialization;

namespace Content.Shared._Orion.Interaction;

[Serializable, NetSerializable]
public sealed class InteractionParticleEvent(NetEntity performer, NetEntity? used, NetEntity target, bool isClientEvent) : EntityEventArgs
{
    public NetEntity Performer = performer;

    public NetEntity? Used = used;

    public NetEntity Target = target;

    /// <summary>
    /// Workaround for event subscription not working w/ the session overload
    /// </summary>
    public bool IsClientEvent = isClientEvent;
}
