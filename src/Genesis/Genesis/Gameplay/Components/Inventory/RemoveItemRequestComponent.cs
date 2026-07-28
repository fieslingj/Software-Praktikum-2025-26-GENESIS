using Genesis.Gameplay.Definitions;

namespace Genesis.Gameplay.Components.Inventory;

/// <summary>
/// Requests that an item should be removed from the inventory
/// </summary>
public struct RemoveItemRequestComponent(ItemType itemType)
{
    public ItemType mItemType = itemType;
}