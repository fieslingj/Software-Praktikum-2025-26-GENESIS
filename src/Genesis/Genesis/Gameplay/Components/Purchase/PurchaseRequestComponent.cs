using Genesis.Gameplay.Definitions;

namespace Genesis.Gameplay.Components.Purchase;

/// <summary>
/// Marks that the player intents a purchase.
/// </summary>
public struct PurchaseRequestComponent(ItemType itemType)
{
    public ItemType mItemType = itemType;
}