namespace Content.Server._Orion.Arrivals.CentComm.Events;

public sealed class CentCommEvent(EntityUid station) : HandledEntityEventArgs
{
    public EntityUid Station { get; } = station;
}
