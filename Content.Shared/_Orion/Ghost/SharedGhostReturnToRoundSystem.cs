using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Shared._Orion.Ghost;

public abstract class SharedGhostReturnToRoundSystem : EntitySystem
{
    [Dependency] protected readonly IConfigurationManager Cfg = default!;
    [Dependency] protected readonly IGameTiming GameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();

        Cfg.OnValueChanged(CCVars.GhostRespawnTime,
            ghostRespawnTime =>
            {
                GhostRespawnTime = TimeSpan.FromSeconds(ghostRespawnTime);
            },
            true);
    }

    protected TimeSpan GhostRespawnTime = new(0, 5, 0);
}
