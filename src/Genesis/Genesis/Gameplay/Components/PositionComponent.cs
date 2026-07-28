using Microsoft.Xna.Framework;

namespace Genesis.Gameplay.Components;

/// <summary>
/// The current position of the entity in the world space.
/// </summary>
public struct PositionComponent(Vector2 position)
{
    public Vector2 Value { get; set; } = position;
}