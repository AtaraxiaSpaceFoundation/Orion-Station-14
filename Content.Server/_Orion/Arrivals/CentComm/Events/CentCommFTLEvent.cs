using Content.Server.Shuttles.Components;

namespace Content.Server._Orion.Arrivals.CentComm.Events;

public sealed class FTLCentCommEvent : EntityEventArgs
{
    public Entity<ShuttleComponent> Source { get; set; }
}
