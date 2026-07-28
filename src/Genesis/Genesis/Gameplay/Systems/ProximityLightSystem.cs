using Microsoft.Xna.Framework;
using Arch.Core;
using Genesis.Gameplay.Components;
using Genesis.Architecture.ECS;

namespace Genesis.Gameplay.Systems;

public class ProximityLightSystem: IUpdateSystem
{
    private static readonly QueryDescription sPlayerDesc = new QueryDescription()
        .WithAll<PlayerTagComponent, PositionComponent>();
    
    private static readonly QueryDescription sObjectDesc = new QueryDescription()
        .WithAll<InteractableComponent, PositionComponent>();

    public void Update(World world, GameTime gameTime)
    {
        // Get the position of the player
        var playerPos = Vector2.Zero; 
        world.Query(in sPlayerDesc,
            (ref PositionComponent pos) =>
            {
                playerPos = pos.Value;
            });
        
        // For the closest entity with ProximityLightComponent where the player is in range, turn the light on.
        // For all other entities with ProximityLightComponent, turn the light off.
        var closestEntity = Entity.Null;
        var minDistanceSq = float.MaxValue;
        
        world.Query(in sObjectDesc,
            (Entity entity, ref PositionComponent pos, ref InteractableComponent interactable) =>
            {
                interactable.LightOn = false;
                var distanceSq = Vector2.DistanceSquared(pos.Value, playerPos);
                
                // Check if distance < radius and if the entity is closer than the current closest entity.
                if (distanceSq < interactable.Radius * interactable.Radius && distanceSq < minDistanceSq)
                {
                    minDistanceSq = distanceSq;
                    closestEntity = entity;
                }
            });

        if (closestEntity == Entity.Null) { return; }

        // Turn on the light of the closest entity.
        ref var closestLight = ref world.Get<InteractableComponent>(closestEntity);
        closestLight.LightOn = true;
    }
}