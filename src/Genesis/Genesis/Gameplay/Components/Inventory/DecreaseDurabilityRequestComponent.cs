namespace Genesis.Gameplay.Components.Inventory;
/// <summary>
/// Request to decrease the durability of active Item
/// </summary>
/// <param name="amount"></param>
public struct DecreaseDurabilityRequestComponent(int amount=1)
{
    public int mAmount = amount;
}