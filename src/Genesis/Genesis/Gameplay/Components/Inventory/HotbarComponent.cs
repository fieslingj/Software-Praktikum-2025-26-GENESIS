using Arch.Core;

namespace Genesis.Gameplay.Components.Inventory;

/// <summary>
/// Stores the hotbar slots as an array.
/// </summary>
public struct HotbarComponent(int activeSlot = 0)
{
    public const int SlotCount = 5;
    public Entity[] Slots { get; } = new Entity[SlotCount];

    public int ActiveSlot { get; set; } = activeSlot;
}