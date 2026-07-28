using System;
using Arch.Core;
using Genesis.Architecture.ECS;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Components.Visuals;
using Genesis.Gameplay.Components.World;
using Genesis.Gameplay.Extensions;
using Microsoft.Xna.Framework;
using Genesis.Gameplay.Navigation;

namespace Genesis.Gameplay.Systems;

/// <summary>
/// Manages door state: checks if enemies remain in the room,
/// handles door opening animation, and manages collision.
/// </summary>
public class DoorSystem : IUpdateSystem
{
    private static readonly QueryDescription sDoorQuery = new QueryDescription()
        .WithAll<DoorComponent, SimpleAnimationComponent>();

    private static readonly QueryDescription sEnemyQuery = new QueryDescription()
        .WithAll<EnemyComponent, HealthComponent>()
        .WithNone<DeathStateComponent>();

    public void Update(World world, GameTime gameTime)
    {
        // Check if there are any living enemies in the room
        var livingEnemyCount = world.CountEntities(in sEnemyQuery);
        var noEnemiesLeft = livingEnemyCount == 0;
        //set room flag
        if(world.GetResource<FloorLayoutComponent>() == null) {return;}
        world.GetResource<FloorLayoutComponent>().CurrentRoom.IsCleared = noEnemiesLeft;

        world.Query(in sDoorQuery,
            (Entity entity, ref DoorComponent door, ref SimpleAnimationComponent animation) =>
            {
                switch (door.State, animation.IsFinished)
                {
                    case (DoorState.Closed, _):
                        door.CanOpen = noEnemiesLeft;
                        break;
                    
                    case (DoorState.Opening, true):
                        door.State = DoorState.Open;
                        if (world.Has<ColliderComponent>(entity))
                        {
                            var pos = world.Get<PositionComponent>(entity).Value;
                            var col = world.Get<ColliderComponent>(entity);
                            var gridMap = world.GetResource<GridMap>();
                            if (gridMap == null)
                            {
                                Console.WriteLine("[DoorSystem] Warning: GridMap resource is missing!");
                            }
                            gridMap?.SetWalkability(pos, col, true);
                            world.Remove<ColliderComponent>(entity);

                            if (world.Has<InteractableComponent>(entity))
                            {
                                ref var interactable = ref world.Get<InteractableComponent>(entity);
                            interactable.LightOn = false;

                            world.Set(entity, interactable);
                            }
                            world.Remove<InteractableComponent>(entity);

                        }
                        break;
                    
                    default:
                        break;
                }
            });
        
    }
}