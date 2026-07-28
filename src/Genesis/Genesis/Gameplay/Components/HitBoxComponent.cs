using Microsoft.Xna.Framework;

namespace Genesis.Gameplay.Components;

/// <summary>
/// Defines the area in which an entity takes damage.
/// Differs from the ColliderComponent, which is responsible  for physical collisions.
/// </summary>

public struct HitBoxComponent(Vector2 size, Vector2 offset)
{
    /// <summary>
    /// The Size of the Hitbox (Height and Width).
    /// </summary>
    public Vector2 Size { get; set; } = size;

    /// <summary>
    /// The offset from the center of the entity.
    /// Allows the hitbox to be positioned independently of the sprite's anchor point,
    /// for example centering it on the torso instead of the feet.
    /// </summary>
    public Vector2 Offset { get; set; } = offset;

    /// <summary>
    /// Calculates the "screen-based" rectangle of the hitbox based on the entity's current position.
    /// </summary>
    public Rectangle GetBounds(Vector2 position)
    {
        var x = (int)(position.X + Offset.X - Size.X / 2f);
        var y = (int)(position.Y + Offset.Y - Size.Y / 2f);

        return new Rectangle(x, y, (int)Size.X, (int)Size.Y);
    }
}