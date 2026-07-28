namespace Genesis.Gameplay.Components.World;

/// <summary>
/// Singleton component that identifies which Save Slot the current game belongs to.
/// </summary>
public struct RunSessionComponent(int? slotIndex = null)
{
    public int? SlotIndex { get; set; } = slotIndex;
    public bool TutorialActive { get; set; } = true;
}