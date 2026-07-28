namespace Genesis.Gameplay.Components;

public enum FaceDirection
{
    North,
    East,
    South,
    West
}

/// <summary>Holds the faced direction.</summary>
public class FaceComponent(FaceDirection faceDirection)
{
    public FaceDirection FaceDirection { get; set; } = faceDirection;
}