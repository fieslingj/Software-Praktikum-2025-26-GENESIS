namespace Genesis.Gameplay.Components;

public readonly struct RoomTransitionTriggerComponent(DoorDirection direction)
{
    public DoorDirection TargetDirection { get; } = direction;
}