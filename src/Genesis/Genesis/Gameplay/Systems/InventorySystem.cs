using Arch.Core;
using Genesis.Architecture.ECS;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Components.Inventory;
using Genesis.Gameplay.Definitions;
using Microsoft.Xna.Framework;

namespace Genesis.Gameplay.Systems;

public class InventorySystem : IUpdateSystem
{
    private static readonly QueryDescription sAddRequestQuery = new QueryDescription()
        .WithAll<AddItemRequestComponent>();
    private static readonly QueryDescription sRemoveRequestQuery = new QueryDescription()
        .WithAll<RemoveItemRequestComponent>();
    private static readonly QueryDescription sClearRequestQuery = new QueryDescription()
        .WithAll<ClearEmptySlotRequestComponent>();

    public void Update(World world, GameTime gameTime)
    {
        // Get the player entity and make sure it has an InventoryComponent
        var playerEntity = Entity.Null;
        world.Query(new QueryDescription().WithAll<InventoryComponent, PlayerTagComponent>(),
            (Entity entity) => { playerEntity = entity; });
        if (playerEntity == Entity.Null)
        {
            return;
        }

        // Get the inventory component
        var inventory = world.Get<InventoryComponent>(playerEntity);
        if (inventory.mSlots == null)
        {
            return;
        }

        // Handle add item requests
        world.Query(in sAddRequestQuery,
            (Entity requestEntity, ref AddItemRequestComponent request) =>
            {
                var success = TryAddItems(world, inventory, request);
                if (success)
                {
                    world.Destroy(requestEntity);
                }
            });

        // Handle remove item requests
        world.Query(in sRemoveRequestQuery,
            (Entity requestEntity, ref RemoveItemRequestComponent request) =>
            {
                for (int i = 0; i < inventory.mSlots.Length; i++)
                {
                    var slotEntity = inventory.mSlots[i];
                    if (slotEntity == Entity.Null) { continue; }

                    ref var id = ref world.Get<ItemIdentificationComponent>(slotEntity);

                    if (id.mType != request.mItemType) { continue; }

                    var shouldDestroy = true;
                    
                    if (world.Has<ItemStackComponent>(slotEntity))
                    {
                        // Reduce the counter
                        ref var itemStack = ref world.Get<ItemStackComponent>(slotEntity);
                        itemStack.mCount -= 1;
                        
                        // Don't destroy, if stack is not empty
                        if (itemStack.mCount > 0)
                        {
                            shouldDestroy = false;
                        }
                        // Don't destroy, if item is still a trigger for remote explosives
                        else if (id.mType == ItemType.RemoteExplosive && world.Has<TriggerTagComponent>(slotEntity))
                        {
                            itemStack.mCount = 0;
                            shouldDestroy = false;
                        }
                    }

                    if (shouldDestroy)
                    {
                        world.Destroy(slotEntity);
                        inventory.mSlots[i] = Entity.Null;
                        CompactInventory(inventory);
                    }
                    break;
                }
                world.Destroy(requestEntity);
            });

        // Handle ClearEmptySlotRequests
        world.Query(in sClearRequestQuery, (Entity requestEntity, ref ClearEmptySlotRequestComponent request) =>
        {
            for (var i = 0; i < inventory.mSlots.Length; i++)
            {
                var slotEntity = inventory.mSlots[i];
                if (slotEntity == Entity.Null)
                {
                    continue;
                }

                if (world.Get<ItemIdentificationComponent>(slotEntity).mType == request.mItemType)
                {
                    world.Destroy(slotEntity);
                    inventory.mSlots[i] = Entity.Null;
                    CompactInventory(inventory);
                    break;
                }
            }
            world.Destroy(requestEntity);
        });
        }

    /// <summary>
    /// Tries to add an item. Returns the success.
    /// </summary>
    /// <remarks>Assumes there is no item that is both stackable and has a lifetime!</remarks>
    private bool TryAddItems(World world, InventoryComponent inventory, AddItemRequestComponent request)
    {
        var itemType = request.mItemType;
        var amount = request.mAmount;
        var hotbarSlot = request.mHotbarSlot;
        var lifetime = request.mLifeTime;
        var def = ItemDefinitions.Get(itemType);
        var stackable = def.Stackable;
        
        if (stackable)
        {
            if (TryStackItem(world, inventory, itemType, amount))
            {
                return true; 
            }
        }
        else if (DoesInventoryContainType(world, inventory, itemType))
        {
            return false; 
        }

        // Else try to find a free slot
        var freeIndex = -1;
        for (var i = 0; i < inventory.mSlots.Length; i++)
        {
            if (inventory.mSlots[i] == Entity.Null)
            {
                freeIndex = i;
                break;
            }
        }

        if (freeIndex == -1) {return false;}

        // Create a new entity for the new item in the inventory
        var newItemEntity = world.Create(
            new ItemIdentificationComponent(itemType)
        );
        
        if (stackable)
        {
            world.Add(newItemEntity, new ItemStackComponent(amount));
        }

        if (lifetime is not null)
        {
            world.Add(newItemEntity, lifetime.Value);
        }

        switch (itemType)
        {
            case ItemType.RemoteExplosive:
                world.Add(newItemEntity, new TriggerTagComponent());
                break;
            case ItemType.Shield:
                world.Add(newItemEntity, new DurabilityComponent(def.Durability));
                break;
        }
        inventory.mSlots[freeIndex] = newItemEntity;

        if (hotbarSlot is { } hotbarIndex)
        {
            AssignItemToHotbar(world, freeIndex, hotbarIndex);
        }

        return true;
    }

    private bool TryStackItem(World world, InventoryComponent inventory, ItemType itemType, int amount)
    {
        // Look at all items in the inventory.
        foreach (var slotEntity in inventory.mSlots)
        {
            if (slotEntity == Entity.Null) { continue; }
            
            ref var id = ref world.Get<ItemIdentificationComponent>(slotEntity);

            if (id.mType != itemType) { continue; }

            // Increase the counter by one if an item of this type was found.
            ref var stack = ref world.Get<ItemStackComponent>(slotEntity);
            stack.mCount += amount;
            return true;
        }
        
        return false;
    }
    
    private void CompactInventory(InventoryComponent @ref)
    {
        int writeIndex = 0;
        
        for (int readIndex = 0; readIndex < @ref.mSlots.Length; readIndex++)
        {
            if (@ref.mSlots[readIndex] != Entity.Null)
            {
                @ref.mSlots[writeIndex] = @ref.mSlots[readIndex];
                writeIndex++;
            }
        }
        
        for (int i = writeIndex; i < @ref.mSlots.Length; i++)
        {
            @ref.mSlots[i] = Entity.Null;
        }
    }

    /// <summary>
    /// Assigns the item in the i-th inventory slot to the active hotbar slot.
    /// </summary>
    public static void AssignItemToHotbar(World gameWorld, int inventoryIndex)
    {
        gameWorld.Query(new QueryDescription().WithAll<PlayerTagComponent, HotbarComponent>(),
            (ref HotbarComponent hotbar) =>
            {
                AssignItemToHotbar(gameWorld, inventoryIndex, hotbar.ActiveSlot);
            });
    }
    
    /// <summary>
    /// Assigns the item in the i-th inventory slot to the specified hotbar slot.
    /// </summary>
    public static void AssignItemToHotbar(World gameWorld, int inventoryIndex, int hotbarIndex)
    {
        gameWorld.Query(
            new QueryDescription().WithAll<PlayerTagComponent, InventoryComponent, HotbarComponent>(),
            (ref HotbarComponent hotbar, ref InventoryComponent inventory) =>
            {
                if (inventoryIndex < 0 || inventoryIndex >= inventory.mSlots.Length) { return; }
                
                var itemToAssign = inventory.mSlots[inventoryIndex];
                
                // Remove the inventory item from the hotbar to avoid duplicates.
                for (var i = 0; i < 5; i++)
                {
                    if (hotbar.Slots[i] == itemToAssign)
                    {
                        hotbar.Slots[i] = Entity.Null;
                    }
                }
                
                if (!(hotbarIndex < 0 || hotbarIndex > hotbar.Slots.Length))
                {
                    hotbar.Slots[hotbarIndex] = itemToAssign;
                }
            });
    }
    
    /// <summary>
    /// Checks if the inventory already contains a specific item type.
    /// </summary>
    public static bool DoesInventoryContainType(World world, InventoryComponent inventory, ItemType typeToCheck)
    {
        foreach (var slotEntity in inventory.mSlots)
        {
            if (slotEntity == Entity.Null) { continue; }

            ref var id = ref world.Get<ItemIdentificationComponent>(slotEntity);
            if (id.mType == typeToCheck)
            {
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Checks if there is at least one empty slot (Entity.Null) in the inventory.
    /// </summary>
    public static bool HasFreeSlot(InventoryComponent inventory)
    {
        if (inventory.mSlots == null) { return false; }
        foreach (var slot in inventory.mSlots)
        {
            if (slot == Entity.Null) { return true; }
        }
        return false;
    }
}