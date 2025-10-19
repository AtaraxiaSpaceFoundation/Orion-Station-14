namespace Content.Shared.GameTicking;

//
// License-Identifier: AGPL-3.0-or-later
//

public sealed class RoundStartedEvent : EntityEventArgs
{
    public int RoundId { get; }
    
    public RoundStartedEvent(int roundId)
    {
        RoundId = roundId;
    }
}
