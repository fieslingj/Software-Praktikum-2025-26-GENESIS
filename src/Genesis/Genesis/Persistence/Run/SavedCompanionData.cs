using System;
using System.Collections.Generic;
using Arch.Core;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Components.Inventory;
using Genesis.Gameplay.Definitions;

namespace Genesis.Persistence.Run;

/// <summary>
/// Represents the complete snapshot of the companion entity's dynamic componentes
/// needed to resume a specific gameplay run.
/// </summary>
[Serializable]
public class SavedCompanionData
{
    public required CompanionComponent Type { get; init; }
    public required PositionComponent Position { get; init; }
    public required HealthComponent Health { get; init; }
    public required AmmoComponent Ammo { get; init; }
    
    // We store the inventory as a list of (ItemType, Count) tuples because Entity references are not serializable
    public List<(ItemType Type, int Count)> Inventory { get; init; } = new();

    public static SavedCompanionData Fetch(World world, Entity companion) => world.FetchCompanionData(companion);
}

public static class SavedCompanionDataMethods
{
    public static List<SavedCompanionData> FetchAllCompanions(this World world)
    {
        var companions = new List<SavedCompanionData>();
        world.Query(new QueryDescription().WithAll<CompanionComponent>(),
            entity => companions.Add(SavedCompanionData.Fetch(world, entity)));

        return companions;
    }
    
    public static SavedCompanionData FetchCompanionData(this World world, Entity companion)
    {
        var inventoryData = new List<(ItemType, int)>();
        if (!world.Has<InventoryComponent>(companion))
        {
            return new SavedCompanionData()
            {
                Type = world.Get<CompanionComponent>(companion),
                Position = world.Get<PositionComponent>(companion),
                Health = world.Get<HealthComponent>(companion),
                Ammo = world.Get<AmmoComponent>(companion),
                Inventory = inventoryData
            };
        }
        
        var inventory = world.Get<InventoryComponent>(companion);
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

        return new SavedCompanionData()
        {
            Type = world.Get<CompanionComponent>(companion),
            Position = world.Get<PositionComponent>(companion),
            Health = world.Get<HealthComponent>(companion),
            Ammo = world.Get<AmmoComponent>(companion),
            Inventory = inventoryData
        };
    }
}