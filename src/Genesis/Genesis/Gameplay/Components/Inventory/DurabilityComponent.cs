namespace Genesis.Gameplay.Components.Inventory;

public struct DurabilityComponent(int max)
{
    public int mCurrent = max;
    public int mMax = max;
}