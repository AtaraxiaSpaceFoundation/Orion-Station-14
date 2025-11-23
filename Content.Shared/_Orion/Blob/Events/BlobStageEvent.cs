namespace Content.Shared._Orion.Blob.Events;

/// <summary>
/// Raised on the station when the blob reaches critical stage.
/// GoobModule should listen for this and call ResponseForce.
/// </summary>
public sealed class BlobCriticalStageEvent : EntityEventArgs
{
    public EntityUid Station { get; }

    public BlobCriticalStageEvent(EntityUid station)
    {
        Station = station;
    }
}
