using Arch.Core;
using Genesis.Architecture.ECS;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Definitions;
using Microsoft.Xna.Framework;

namespace Genesis.Gameplay.Systems;

public class VisualStateSystem : IUpdateSystem
{
    private static readonly QueryDescription sQuery = new QueryDescription()
        .WithAll<SpriteComponent, StatusComponent>();

    public void Update(World world, GameTime gameTime)
    {
        world.Query(in sQuery, (Entity entity, ref SpriteComponent sprite, ref StatusComponent status) =>
        {
            var finalColor = Color.White;

            if (world.Has<PlayerTagComponent>(entity) && world.Has<ActiveShieldComponent>(entity))
            {
                finalColor = Color.Cyan;
            }

            foreach (var effect in status.Types)
            {
                if (effect.Item1 is not (StatusType.AcidSour or StatusType.InAcid)) { continue; }

                finalColor = Color.Green;
                break;
            }
            
            // Boss enemy is low enough to be chipped
            if (world.Has<EnemyComponent>(entity) && world.Has<HealthComponent>(entity))
            {
                ref var enemyComp = ref world.Get<EnemyComponent>(entity);
                var def = EnemyDefinitions.Get(enemyComp.Type);
                if (def.IsMutant)
                {
                    ref var health = ref world.Get<HealthComponent>(entity);
                    if (health.Current < 0.4 * def.MaxHealth)
                    {
                        finalColor = Color.Purple;
                    }
                }
            }

            sprite.mColor = finalColor;
        });
    }
}