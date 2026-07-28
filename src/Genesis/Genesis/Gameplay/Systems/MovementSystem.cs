using System;
using Genesis.Architecture.ECS;
using Microsoft.Xna.Framework;
using Arch.Core;
using Genesis.Gameplay.Components;
using System.Collections.Generic;
using Genesis.Gameplay.Navigation;

namespace Genesis.Gameplay.Systems;

/// <summary>
/// This System handles all movement (applying velocity to position) and resolves collisions against static geometry.
/// It requires PositionComponent, VelocityComponent, and ColliderComponent for moving entities.
/// </summary>
public class MovementSystem(SpatialHash spatialHash) : IUpdateSystem
{
    private readonly List<SpatialEntry> mNearbyWalls = new(16);
    
    private static readonly QueryDescription sMovingWithStaminaQuery = new QueryDescription()
        .WithAll<PositionComponent, VelocityComponent, ColliderComponent, StateComponent, StaminaComponent>();

    private static readonly QueryDescription sMovingWithoutStaminaQuery = new QueryDescription()
        .WithAll<PositionComponent, VelocityComponent, ColliderComponent, StateComponent>()
        .WithNone<StaminaComponent>();

    private static readonly QueryDescription sMovingOnlyProjectile = new QueryDescription()
        .WithAll<PositionComponent,VelocityComponent,ProjectileComponent>();

    private const float SprintSpeedMult = 1.8f;

    public void Update(World world, GameTime gameTime)
    {
        var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // entities that can move (have VelocityComponent)
        world.Query(in sMovingWithStaminaQuery,
            (ref PositionComponent position, ref VelocityComponent velocity,
                ref ColliderComponent collider, ref StateComponent state, ref StaminaComponent stamina) =>
            {
                var speed = velocity.Value;

                // If actor is sprinting, increase speed if stamina is available
                if (state.Current == ActorState.Sprinting)
                {
                    if (stamina.Current > 0f)
                    {
                        speed *= SprintSpeedMult;
                    }
                    else
                    {
                        // If stamina is depleted, revert to walking
                        state.Current = ActorState.Walking;
                    }
                }

                // Calculation of movement and collision
                var deltaMovement = velocity.Direction * speed * deltaTime;

                // Move and resolve collisions
                ResolveMovement(ref position, ref velocity, ref collider, deltaMovement);
            });

        // Entities that can move but have no StaminaComponent
        world.Query(in sMovingWithoutStaminaQuery,
            (ref PositionComponent position, ref VelocityComponent velocity,
                ref ColliderComponent collider, ref StateComponent state) =>
            {
                var speed = velocity.Value;

                // Higher speed if sprinting
                if (state.Current == ActorState.Sprinting)
                {
                    speed *= SprintSpeedMult;
                }

                // Calculation of movement and collision
                var deltaMovement = velocity.Direction * speed * deltaTime;

                // Move and resolve collisions
                ResolveMovement(ref position, ref velocity, ref collider, deltaMovement);
            });
        // Entities that can move but nothing else(Projectile)
        world.Query(in sMovingOnlyProjectile,
            (ref PositionComponent position, ref VelocityComponent velocity) =>
            {

                var speed = velocity.Value;


                // Calculation of movement and collision
                var deltaMovement = velocity.Direction * speed * deltaTime;

                // Move
                position.Value += deltaMovement;
            });

    }

    private void ResolveMovement(
        ref PositionComponent position,
        ref VelocityComponent velocity,
        ref ColliderComponent collider,
        Vector2 deltaMovement)
    {
        var queryRect = collider.GetAabb(position.Value);
        // Puffer to use the same rect for y-movement after the x-movement.
        var inflateX = (int)Math.Ceiling(Math.Abs(deltaMovement.X)) + 2;
        var inflateY = (int)Math.Abs(deltaMovement.Y) + 2;
        queryRect.Inflate(inflateX, inflateY);

        mNearbyWalls.Clear();
        spatialHash.GetEntitiesInRect(queryRect, mNearbyWalls);
        
        // check horizontal movement
        var simulatedMovement = velocity.Direction;
        var sepX = new Vector2(deltaMovement.X, 0);
        position.Value += sepX;
        
        if (CheckCollision(position.Value, collider, mNearbyWalls))
        {
            position.Value -= sepX;
            simulatedMovement.X = 0;
        }

        // 2. Y-Achse
        var sepY = new Vector2(0, deltaMovement.Y);
        position.Value += sepY;

        if (CheckCollision(position.Value, collider, mNearbyWalls))
        {
            position.Value -= sepY;
            simulatedMovement.Y = 0;
        }
        
        if (simulatedMovement != velocity.Direction) { velocity.Direction = simulatedMovement; }
    }

    private bool CheckCollision(Vector2 currentPos, ColliderComponent movingCol, IReadOnlyList<SpatialEntry> candidates)
    {
        // Get the AABB of the moving entity
        var movingAabb = movingCol.GetAabb(currentPos);

        var count = candidates.Count;
        for (var i = 0; i < count; i++)
        {
            var entry = candidates[i];
            
            if ((entry.mFlags & SpatialFlags.Static) == 0) { continue; }
            if (movingAabb.Intersects(entry.mAabb)) { return true; }
        }
        return false;
    }
}