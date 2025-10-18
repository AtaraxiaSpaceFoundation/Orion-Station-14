using Content.Server._Orion.Explosion.EntitySystems;

namespace Content.Server._Orion.Explosion.Components;

/// <summary>
/// Grenades that, when triggered, explode into decals.
/// </summary>
[RegisterComponent, Access(typeof(DecalGrenadeSystem))]
public sealed partial class DecalGrenadeComponent : Component
{
    /// <summary>
    /// The kinds of decals to spawn on explosion.
    /// </summary>
    [DataField]
    public List<string> DecalPrototypes = new();

    /// <summary>
    /// The number of decals to spawn upon explosion.
    /// </summary>
    [DataField]
    public int DecalCount = 25;

    /// <summary>
    /// The radius in which decals will spawn around the explosion center.
    /// </summary>
    [DataField]
    public float DecalRadius = 3f;
}
