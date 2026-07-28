using System.Runtime.CompilerServices;
using Arch.Core;
using Genesis.Gameplay.Components;

namespace Genesis.Gameplay.Extensions;

public static class WorldExtensions
{
    /// <summary>
    /// Returns the first Entity found that matches the query.
    /// Useful for Singletons (Player, Map, RunSession, ...)
    /// </summary>
    /// <returns>The first Entity found, or Entity.Null if none found.</returns>
    public static Entity GetFirstEntity(this World world, in QueryDescription queryDescription)
    {
        var query = world.Query(in queryDescription);
        foreach (var chunk in query.GetChunkIterator())
        {
            if (chunk.Count > 0) {return chunk.Entities[0];}
        }
        
        return Entity.Null;
    }

    /// <summary>
    /// Safely removes component T from the entity if it exists.
    /// Prevents exceptions when trying to remove a missing component.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RemoveIfExists<T>(this World world, Entity entity)
    {
        if (world.Has<T>(entity))
        {world.Remove<T>(entity);}
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RemoveIfExists<T1, T2>(this World world, Entity entity)
    {
        var hasT1 = world.Has<T1>(entity);
        var hasT2 = world.Has<T2>(entity);

        switch (hasT1, hasT2)
        {
            case (true, true):
                world.Remove<T1, T2>(entity);
                break;
            
            case (true, false):
                world.Remove<T1>(entity);
                break;
                
            case (false, true):
                world.Remove<T2>(entity);
                break;
            
            default:
                return;
        }
    }
    
    /// <summary>
    /// Safely checks if an entity is alive and has the EnemyComponent.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEnemy(this World world, Entity entity)
    {
        return entity != Entity.Null && world.IsAlive(entity) && world.Has<EnemyComponent>(entity);
    }

    /// <summary>
    /// Safely checks if an entity is the player or a friendly companion.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsFriendly(this World world, Entity entity)
    {
        return entity != Entity.Null && world.IsAlive(entity) && 
               (world.Has<PlayerTagComponent>(entity) || world.Has<CompanionComponent>(entity));
    }
}