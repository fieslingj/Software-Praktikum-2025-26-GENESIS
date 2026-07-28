namespace Genesis.Gameplay.Components.Inventory;

/// <summary>
/// The count of the item the inventory.
/// </summary>
public struct ItemStackComponent(int count=1)
{
    public int mCount = count;
}