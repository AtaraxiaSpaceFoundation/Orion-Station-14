using System.Numerics;
using Content.Server._Orion.Explosion.Components;
using Content.Server.Decals;
using Content.Server.Explosion.EntitySystems;
using Robust.Shared.Random;

namespace Content.Server._Orion.Explosion.EntitySystems
{
    public sealed class DecalGrenadeSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly DecalSystem _decalSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DecalGrenadeComponent, TriggerEvent>(OnFragTrigger);
        }

        /// <summary>
        /// Triggered when the grenade explodes, spawns decals around the explosion.
        /// </summary>
        private void OnFragTrigger(Entity<DecalGrenadeComponent> entity, ref TriggerEvent args)
        {
            var component = entity.Comp;

            if (component.DecalPrototypes.Count == 0)
                return;

            SpawnDecals(entity.Owner, component);
            args.Handled = true;
        }

        /// <summary>
        /// Spawns decals in radius around the grenade explosion.
        /// </summary>
        private void SpawnDecals(EntityUid grenadeUid, DecalGrenadeComponent component)
        {
            if (!TryComp(grenadeUid, out TransformComponent? grenadeXform))
                return;

            var grenadePosition = grenadeXform.Coordinates;

            for (var i = 0; i < component.DecalCount; i++)
            {
                var radius = component.DecalRadius * (_random.NextFloat() + 0.1f);
                var angle = _random.NextFloat() * MathF.Tau;

                var offset = new Vector2(
                    radius * MathF.Cos(angle),
                    radius * MathF.Sin(angle));

                var decalPosition = grenadePosition.Offset(offset);

                if (component.DecalPrototypes.Count == 0)
                    continue;

                var decalPrototype = component.DecalPrototypes[_random.Next(component.DecalPrototypes.Count)];

                _decalSystem.TryAddDecal(
                    decalPrototype,
                    decalPosition,
                    out _,
                    cleanable: true);
            }
        }
    }
}
