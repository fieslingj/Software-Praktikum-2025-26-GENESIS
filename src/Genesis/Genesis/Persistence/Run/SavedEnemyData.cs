using System;
using System.Collections.Generic;
using Arch.Core;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Components.Inventory;
using Genesis.Gameplay.Definitions;

namespace Genesis.Persistence.Run;

/// <summary>
/// Represents the complete snapshot of the enemy entity's dynamic componentes
/// needed to resume a specific gameplay run.
/// </summary>
[Serializable]
public class SavedEnemyData
{
    public required EnemyComponent Type { get; init; }
    public required PositionComponent Position { get; init; }
    public required HealthComponent Health { get; init; }
    public required AmmoComponent Ammo { get; init; }
    
    // We store the inventory as a list of (ItemType, Count) tuples because Entity references are not serializable
    public List<(ItemType Type, int Count)> Inventory { get; init; } = new();
}

public static class SavedEnemyDataMethods
{
    private static readonly QueryDescription sQuery = new QueryDescription().WithAll<EnemyComponent>();
    public static List<SavedEnemyData> FetchAllEnemies(this World world)
    {
        var enemies = new List<SavedEnemyData>();
        world.Query(sQuery, entity => enemies.Add(world.FetchEnemyData(entity)));
        return enemies;
    }

    private static SavedEnemyData FetchEnemyData(this World world, Entity enemy)
    {
        var inventoryData = new List<(ItemType, int)>();
        if (!world.Has<InventoryComponent>(enemy))
        {
            return new SavedEnemyData()
            {
                Type = world.Get<EnemyComponent>(enemy),
                Position = world.Get<PositionComponent>(enemy),
                Health = world.Get<HealthComponent>(enemy),
                Ammo = world.Get<AmmoComponent>(enemy),
                Inventory = inventoryData
            };
        }

        var inventory = world.Get<InventoryComponent>(enemy);
        foreach (var slotEntity in inventory.mSlots)
        {
            if (slotEntity == Entity.Null || !world.Has<ItemIdentificationComponent>(slotEntity)) { continue; }
            
            var id = world.Get<ItemIdentificationComponent>(slotEntity);
            var count = 1;
            if (world.Has<ItemStackComponent>(slotEntity))
            {
                count = world.Get<ItemStackComponent>(slotEntity).mCount;
            }
            inventoryData.Add((id.mType, count));
        }

        return new SavedEnemyData()
        {
            Type = world.Get<EnemyComponent>(enemy),
            Position = world.Get<PositionComponent>(enemy),
            Health = world.Get<HealthComponent>(enemy),
            Ammo = world.Get<AmmoComponent>(enemy),
            Inventory = inventoryData
        };
    }
}