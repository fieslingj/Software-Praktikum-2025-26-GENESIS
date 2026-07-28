using System;
using System.Collections.Generic;
using Arch.Core;
using Genesis.Gameplay.Components;
using Microsoft.Xna.Framework;

namespace Genesis.Persistence.Run;

/// <summary>
/// Represents the snapshot of a trap's state.
/// </summary>
[Serializable]
public class SavedTrapData
{
    public required TrapType Type { get; init; }
    public required float Damage { get; init; }
    public required float Radius { get; init; }
    public required bool IsActive { get; init; }
    public required PositionComponent Position { get; init; }
    public required Vector2 EffectPosition { get; init; }
}

public static class SavedTrapDataMethods
{
    private static readonly QueryDescription sQuery = new QueryDescription().WithAll<TrapComponent>();
    
    public static List<SavedTrapData> FetchAllTraps(this World world)
    {
        var traps = new List<SavedTrapData>();
        world.Query(sQuery, entity => traps.Add(world.FetchTrapData(entity)));
        return traps;
    }
    
    private static SavedTrapData FetchTrapData(this World world, Entity trap)
    {
        var trapComp = world.Get<TrapComponent>(trap);
        var trapPos = world.Get<PositionComponent>(trap);
        
        var effectPos = Vector2.Zero;
        if (trapComp.EffectEntity != Entity.Null && world.IsAlive(trapComp.EffectEntity) && world.Has<PositionComponent>(trapComp.EffectEntity))
        {
            effectPos = world.Get<PositionComponent>(trapComp.EffectEntity).Value;
        }

        return new SavedTrapData()
        {
            Type = trapComp.Type,
            Damage = trapComp.Damage,
            Radius = trapComp.Radius,
            IsActive = trapComp.IsActive,
            Position = trapPos,
            EffectPosition = effectPos
        };
    }
}