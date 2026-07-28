using Arch.Core;
using Genesis.Architecture;
using Genesis.Architecture.ECS;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Definitions;
using Genesis.GameStates.Gameplay;
using Microsoft.Xna.Framework;

namespace Genesis.Gameplay.Systems;

public class DeathSystem(GameStateManager stateManager) : IUpdateSystem
{
    private GameStateManager mGameStateManager = stateManager;

    private readonly QueryDescription mNewlyDeadQuery = new QueryDescription()
        .WithAll<HealthComponent>()
        .WithNone<DeathStateComponent>();

    private readonly QueryDescription mDeadQuery = new QueryDescription()
        .WithAll<DeathStateComponent, StateComponent>();

    private readonly QueryDescription mSessionQuery = new QueryDescription()
        .WithAll<RunStatsComponent>();

    public void Update(World world, GameTime gameTime)
    {
        SetNewlyDead(world);
        SetActorState(world);
    }

    private void SetNewlyDead(World world)
    {
        world.Query(in mNewlyDeadQuery, (Entity entity, ref HealthComponent health) =>
        {
            if (health.Current > 0) { return; }

            // Mark as dead
            var lingerTime = DetermineLingerTime(world, entity);
            world.Add(entity, new DeathStateComponent(lingerTime));

            // Cleanup unused Components
            world.RemoveRange(entity, [
                typeof(ColliderComponent),
                typeof(VelocityComponent),
                typeof(HitBoxComponent),
            ]);

            //handle specific deaths
            if (world.Has<EnemyComponent>(entity))
            {
                world.Query(in mSessionQuery, (ref RunStatsComponent runStats) =>
                {
                    if (runStats.RunType == (int)RunType.Techdemo)
                    {
                        return;
                    }
                    runStats.EnemiesDefeated += 1;
                });

                if (world.Get<EnemyComponent>(entity).Type == EnemyType.Ceo)
                {
                    mGameStateManager.PushState(new WinState());
                }
            }
        });
    }

    private static float DetermineLingerTime(World world, Entity entity)
    {
        // The player persists forever (until GameOverState handles it)
        // All other entities linger for the same constant amount.
        return world.Has<PlayerTagComponent>(entity) ? float.MaxValue : 3.0f;
    }

    private void SetActorState(World world)
    {
        world.Query(in mDeadQuery, (ref StateComponent actorState) =>
        {
            actorState.Current = ActorState.Dead;
        });
    }
}