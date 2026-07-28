using System;
using System.Collections.Generic;
using Arch.Core;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Components.Inventory;
using Genesis.Gameplay.Definitions;

namespace Genesis.Persistence.Run;

[Serializable]
public class SavedInventoryData
{
    /// <summary>Holds a reference of the item count of every type</summary>
    public required Dictionary<ItemType, SavedItemProperties> Items { get; init; }
    public required int InventoryMaxSize { get; init; }
    
    public required bool HasHotbar { get; init; } = false;
    public required ItemType[] Hotbar { get; init; } = new ItemType[5];
    public required int ActiveSlot { get; init; }

    public static SavedInventoryData Fetch(World world, Entity entity)
    {
        var inventory = world.Get<InventoryComponent>(entity);
        var items = new Dictionary<ItemType, SavedItemProperties>();
        foreach (var item in inventory.mSlots)
        {
            if (!world.IsAlive(item)) {continue;}

            var type = world.Get<ItemIdentificationComponent>(item).mType;
            var properties = new SavedItemProperties
            {
                Amount = world.Has<ItemStackComponent>(item) ? world.Get<ItemStackComponent>(item).mCount : 1,
                LifeTime = world.Has<LifeTimeComponent>(item) ? world.Get<LifeTimeComponent>(item) : null
            };

            items[type] = properties;
        }

        var hotbar = new ItemType[5];
        var hasHotbar = world.Has<HotbarComponent>(entity);
        
        var activeSlot = -1;
        if (!hasHotbar)
            return new SavedInventoryData()
            {
                InventoryMaxSize = inventory.mSlots.Length,
                Items = items,
                HasHotbar = false,
                Hotbar = hotbar,
                ActiveSlot = activeSlot,
            };
        {
            var hotbarComponent = world.Get<HotbarComponent>(entity);
            for (var i = 0; i < 5; i++)
            {
                if (!world.IsAlive(hotbarComponent.Slots[i])) {continue;}

                var item = hotbarComponent.Slots[i];
                var type = world.Get<ItemIdentificationComponent>(item).mType;
                hotbar[i] = type;
                activeSlot = hotbarComponent.ActiveSlot;
            }
        }

        return new SavedInventoryData()
        {
            InventoryMaxSize = inventory.mSlots.Length,
            Items = items,
            HasHotbar = true,
            Hotbar = hotbar,
            ActiveSlot = activeSlot,
        };
    }
}

[Serializable]
public readonly struct SavedItemProperties(int amount, LifeTimeComponent? lifeTime = null)
{
    public int Amount { get; init; } = amount;
    public LifeTimeComponent? LifeTime { get; init; } = lifeTime;
    
    public SavedItemProperties() : this(1) {}
}