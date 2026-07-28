using System;
using Arch.Core;

namespace Genesis.Gameplay.Components.Inventory;

/// <summary>
/// Stores the inventory slots as an array.
/// </summary>
public struct InventoryComponent
{
    public Entity[] mSlots;

    public InventoryComponent(int maxSlots)
    {
        mSlots = new Entity[maxSlots];
        Array.Fill(mSlots, Entity.Null);
    }
}