using Microsoft.Xna.Framework;
namespace Genesis.Gameplay.Components;

public readonly struct LinePositionComponent(Vector2 start, Vector2 end,float thickness,Color color)
{
    public Vector2 Start { get; } = start;
    public Vector2 End { get; } = end;
    public float Thickness { get; } = thickness;
    public Color Color { get; } = color;
}