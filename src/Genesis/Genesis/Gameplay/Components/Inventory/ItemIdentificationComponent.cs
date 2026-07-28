using Genesis.Gameplay.Definitions;

namespace Genesis.Gameplay.Components.Inventory;

/// <summary>
/// Stores the type of the item entity.
/// </summary>
public struct ItemIdentificationComponent(ItemType type)
{
    public ItemType mType = type;
}