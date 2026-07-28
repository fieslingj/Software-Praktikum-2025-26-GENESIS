using Genesis.Gameplay.Definitions;

namespace Genesis.Gameplay.Components.Inventory;

/// <summary>
/// Request to clear an empty slot (e.g. after remote explosive detonation)
/// </summary>
public struct ClearEmptySlotRequestComponent(ItemType itemType)
{
    public ItemType mItemType = itemType;
}