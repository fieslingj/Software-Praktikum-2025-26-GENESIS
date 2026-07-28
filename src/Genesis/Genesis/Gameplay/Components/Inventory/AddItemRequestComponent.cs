using Genesis.Gameplay.Definitions;
using Genesis.Persistence.Run;

namespace Genesis.Gameplay.Components.Inventory;

/// <summary>
/// Requests that a number of items of a specific type should be added to the inventory
/// If lifetimeSeconds >0: After the lifetime, the 
/// </summary>
public struct AddItemRequestComponent(ItemType itemType, int amount=1, int? hotbarSlot=null, LifeTimeComponent? lifetime=null)
{
    public ItemType mItemType = itemType;
    public int mAmount = amount;
    public int? mHotbarSlot = hotbarSlot;
    public LifeTimeComponent? mLifeTime = lifetime;
    
    public AddItemRequestComponent(ItemType type, SavedItemProperties properties, int? hotbarSlot = null)
        : this(type, properties.Amount, hotbarSlot, properties.LifeTime) {}
}