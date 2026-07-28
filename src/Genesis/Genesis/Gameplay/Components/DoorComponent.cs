namespace Genesis.Gameplay.Components;

/// <summary>
/// Marks an entity as a door that can be opened through interaction.
/// </summary>
public struct DoorComponent(DoorDirection door, bool isOpen = false)
{
    public DoorState State { get; set; } = isOpen ? DoorState.Open : DoorState.Closed;
    public bool CanOpen { get; set; } = false;
    public DoorDirection Location { get; } = door;
}

public enum DoorDirection
{
    North,
    East,
    South,
    West,
    Elevator,
}

public enum DoorState
{
    Closed,
    Opening,
    Open,
}