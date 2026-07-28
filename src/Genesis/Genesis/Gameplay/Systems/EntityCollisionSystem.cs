using Genesis.Architecture.ECS;
using Microsoft.Xna.Framework;
using Arch.Core;
using Genesis.Gameplay.Navigation;
using Genesis.Gameplay.Components;
using System.Collections.Generic;

namespace Genesis.Gameplay.Systems;

/// <summary>
/// Resolves collision between dynamic entities.
/// </summary>
public class EntityCollisionSystem(SpatialHash spatialHash) : IUpdateSystem
{
    // Query for dynamic objects
    private static readonly QueryDescription sDynamicQuery = new QueryDescription()
        .WithAll<PositionComponent, ColliderComponent, VelocityComponent>();

    // Query for walls
    private static readonly QueryDescription sStaticQuery = new QueryDescription()
        .WithAll<PositionComponent, ColliderComponent>()
        .WithNone<VelocityComponent, TriggerColliderTagComponent, RoomTransitionTriggerComponent>();
    
    // Query for hitboxes
    private static readonly QueryDescription sHitBoxQuery = new QueryDescription()
        .WithAll<PositionComponent, HitBoxComponent>();

    // Caches
    private readonly List<SpatialEntry> mNearbyEntities = new(32);
    private readonly List<SpatialEntry> mNearbyWalls = new(16);

    public void Update(World world, GameTime gameTime)
    {
        spatialHash.Clear();

        // Insert static collider entities
        world.Query(in sStaticQuery, (Entity entity, ref PositionComponent pos, ref ColliderComponent col) =>
        {
            spatialHash.Insert(entity, col.GetAabb(pos.Value), pos.Value, SpatialFlags.Static); 
        });

        // Insert Dynamic collider entities
        world.Query(in sDynamicQuery, (Entity entity, ref PositionComponent pos, ref ColliderComponent col) =>
        {
            ResolveStaticCollision(world, ref pos, col);
            var flags = SpatialFlags.Dynamic;

            if (world.Has<CompanionComponent>(entity) ||
                world.Has<PlayerTagComponent>(entity)) { flags |= SpatialFlags.Friend; }
            else if(world.Has<EnemyComponent>(entity)) { flags |= SpatialFlags.Enemy; }

            spatialHash.Insert(entity, col.GetAabb(pos.Value), pos.Value, flags);
        });
        
        // Insert Hitbox entities
        world.Query(in sHitBoxQuery, (Entity entity, ref PositionComponent pos, ref HitBoxComponent hitbox) =>
        {
            var rect = hitbox.GetBounds(pos.Value);
            spatialHash.Insert(entity, rect, pos.Value, SpatialFlags.Hitbox);
        });
        
        world.Query(in sDynamicQuery, (Entity entityA, ref PositionComponent posA, ref ColliderComponent colA) =>
        {
            if (colA.IsSensor) { return; }
            
            Rectangle rectA = colA.GetAabb(posA.Value);

            mNearbyEntities.Clear();
            spatialHash.GetEntitiesInRect(rectA, mNearbyEntities);

            foreach (var entryB in mNearbyEntities)
            {
                var entityB = entryB.mEntity;

                if (entityA.Id >= entityB.Id) { continue; }
                if ((entryB.mFlags & SpatialFlags.Dynamic) == 0) { continue; }
                if (!rectA.Intersects(entryB.mAabb)) { continue; }

                ref var posB = ref world.Get<PositionComponent>(entityB);
                ref var colB = ref world.Get<ColliderComponent>(entityB);

                ResolveEntityCollision(world, ref posA, ref colA, ref posB, in colB, entryB.mAabb);
            }
        });
    }

    private void ResolveEntityCollision(World world,
        ref PositionComponent posA, ref ColliderComponent colA,
        ref PositionComponent posB, in ColliderComponent colB,
        Rectangle rectB)
    {
        Rectangle rectA = colA.GetAabb(posA.Value);

        Rectangle intersection = Rectangle.Intersect(rectA, rectB);
        if (intersection.IsEmpty)
        {
            return;
        }

        float sepX = 0;
        float sepY = 0;

        var centerAx = rectA.X + (rectA.Width * 0.5f);
        var centerBx = rectB.X + (rectB.Width * 0.5f);
        var centerAy = rectA.Y + (rectA.Height * 0.5f);
        var centerBy = rectB.Y + (rectB.Height * 0.5f);

        if (intersection.Width < intersection.Height)
        {
            var amount = intersection.Width * 0.5f;
            sepX = (centerAx < centerBx) ? -amount : amount;
        }
        else
        {
            var amount = intersection.Height * 0.5f;
            sepY = (centerAy < centerBy) ? -amount : amount;
        }

        // X Movement
        if (sepX != 0)
        {
            var newPosA = posA.Value;
            newPosA.X += sepX;
            if (!CheckWallOverlap(world, newPosA, colA))
            {
                posA.Value = newPosA;
            }

            var newPosB = posB.Value;
            newPosB.X -= sepX;
            if (!CheckWallOverlap(world, newPosB, colB))
            {
                posB.Value = newPosB;
            }
        }

        // Y Movement
        if (sepY != 0)
        {
            var newPosA = posA.Value;
            newPosA.Y += sepY;
            if (!CheckWallOverlap(world, newPosA, colA))
            {
                posA.Value = newPosA;
            }

            var newPosB = posB.Value;
            newPosB.Y -= sepY;
            if (!CheckWallOverlap(world, newPosB, colB))
            {
                posB.Value = newPosB;
            }
        }
    }
    
    private bool CheckWallOverlap(World world, Vector2 pos, ColliderComponent col)
    {
        var aabb = col.GetAabb(pos);
        
        mNearbyWalls.Clear();
        spatialHash.GetEntitiesInRect(aabb, mNearbyWalls);

        foreach (var entry in mNearbyWalls)
        {
            if ((entry.mFlags & SpatialFlags.Static) == 0) { continue; }
            if (!entry.mAabb.Intersects(aabb)) { continue; }
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Checks if an entity is inside a static collider (wall, prop)
    /// </summary>
    private void ResolveStaticCollision(World world, ref PositionComponent pos, ColliderComponent col)
    {
        var aabb = col.GetAabb(pos.Value);
        
        mNearbyWalls.Clear();
        spatialHash.GetEntitiesInRect(aabb, mNearbyWalls);

        foreach (var entry in mNearbyWalls)
        {
            if ((entry.mFlags & SpatialFlags.Static) == 0) { continue; }

            if (!entry.mAabb.Intersects(aabb)) { continue; }

            Rectangle wallRect = entry.mAabb;
            Rectangle intersection = Rectangle.Intersect(aabb, wallRect);
            if (intersection.IsEmpty) { continue; }

            float sepX = 0;
            float sepY = 0;

            var centerEntityX = aabb.X + (aabb.Width * 0.5f);
            var centerWallX = wallRect.X + (wallRect.Width * 0.5f);
            var centerEntityY = aabb.Y + (aabb.Height * 0.5f);
            var centerWallY = wallRect.Y + (wallRect.Height * 0.5f);

            if (intersection.Width < intersection.Height)
            {
                var amount = intersection.Width;
                sepX = (centerEntityX < centerWallX) ? -amount : amount;
            }
            else
            {
                var amount = intersection.Height;
                sepY = (centerEntityY < centerWallY) ? -amount : amount;
            }

            var newPos = pos.Value;
            
            newPos.X += sepX;
            newPos.Y += sepY;

            pos.Value = newPos;

            aabb.X += (int)sepX;
            aabb.Y += (int)sepY;
        }
    }
}