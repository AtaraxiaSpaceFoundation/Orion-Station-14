using Content.Shared.CCVar;
using Robust.Shared.Configuration;

namespace Content.Shared._Orion.Ghost;

public abstract class SharedGhostReturnToRoundSystem : EntitySystem
{
    [Dependency] protected readonly IConfigurationManager ConfigurationManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        ConfigurationManager.OnValueChanged(CCVars.GhostRespawnTime,
            ghostRespawnTime =>
            {
                GhostRespawnTime = TimeSpan.FromMinutes(ghostRespawnTime);
            },
            true);
    }

    protected TimeSpan GhostRespawnTime = new(0, 42, 0);
}
