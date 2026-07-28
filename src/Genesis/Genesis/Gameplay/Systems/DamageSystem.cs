using Genesis.Architecture.ECS;
using Arch.Core;
using Genesis.Gameplay.Components;
using Microsoft.Xna.Framework;

namespace Genesis.Gameplay.Systems;

/// <summary>
/// Subtracts all accumulated damage from an entity's current health.
/// If the resulting health is 0 or below, the entity state is set to 'Dead'.
/// </summary>
public class DamageSystem(DrawSystem drawSystem) : IUpdateSystem
{
    private static readonly QueryDescription sQueryDesc = new QueryDescription()
        .WithAll<DamageBufferComponent, HealthComponent, StateComponent>();

    private static readonly QueryDescription sMSessionQuery =
        new QueryDescription().WithAll<RunStatsComponent>();

    public void Update(World world, GameTime gameTime)
    {
        world.Query(in sQueryDesc,
            (Entity entity,
                ref DamageBufferComponent damageBuffer,
                ref HealthComponent healthComponent,
                ref StateComponent stateComponent) =>
            {
                // Sum all hits.f
                var damageSum = 0f;
                for (var i = 0; i < damageBuffer.HitsCount; ++i)
                {
                    damageSum += damageBuffer.mHits[i].Value;
                }

                // TODO for debug, remove later
                if (world.Has<PlayerTagComponent>(entity))
                {
                    drawSystem.PlayerDamage += damageSum;
                    // Add value to run stats
                    world.Query(in sMSessionQuery,
                        (ref RunStatsComponent runStats) =>
                        {
                            // Exit if techdemo run
                            if (runStats.RunType == (int)RunType.Techdemo)
                            {
                                return;
                            }
                            runStats.DamageTaken += damageSum;
                        });
                }

                if (world.Has<EnemyComponent>(entity))
                {
                    drawSystem.EnemyDamage += damageSum;

                    // Add value to run stats
                    world.Query(in sMSessionQuery,
                        (ref RunStatsComponent runStats) =>
                        {
                            // Exit if techdemo run
                            if (runStats.RunType == (int)RunType.Techdemo)
                            {
                                return;
                            }
                            runStats.DamageDealt += damageSum;
                        });
                }

                // Subtract the sum from the health.
                healthComponent.Current -= damageSum;

                // If health <=0, the entity is dead.
                if (healthComponent.Current <= 0f)
                {
                    stateComponent.Current = ActorState.Dead;
                }
                // Remove the DamageBufferComponent so its damage is not applied again next frame.
                world.Remove<DamageBufferComponent>(entity);
            });
    }
}