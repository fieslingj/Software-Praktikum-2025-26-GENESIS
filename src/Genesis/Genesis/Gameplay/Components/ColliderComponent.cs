using System;
using Microsoft.Xna.Framework;

namespace Genesis.Gameplay.Components;
/// <summary>
/// Axis-aligned bounding box (AABB) with size and offset for collision detection.
/// </summary>
public struct ColliderComponent(Vector2 size, Vector2? offset = null, bool isSensor = false)
{
    public Vector2 Size { get; } = size;
    public Vector2 Offset { get; } = offset ?? Vector2.Zero;
    public bool IsSensor { get; } = isSensor;
    public float Radius { get; } = (float)Math.Sqrt(size.X * size.X + size.Y * size.Y) / 2f;

    // Pre calculate values used in GetAabb.
    private readonly float mHalfWidth = size.X * 0.5f;
    private readonly float mHalfHeight = size.Y * 0.5f;
    private readonly int mCeiledWidth = (int)MathF.Ceiling(size.X);
    private readonly int mCeiledHeight = (int)MathF.Ceiling(size.Y);

    /// <summary>
    /// returns the axis-aligned bounding box (AABB) for the collider at the given position.
    /// </summary>
    public readonly Rectangle GetAabb(Vector2 position)

    {
        // convert to Rectangle with flooring and ceiling to ensure proper coverage
        return new Rectangle(
            (int)MathF.Floor(position.X + Offset.X - mHalfWidth),
            (int)MathF.Floor(position.Y + Offset.Y - mHalfHeight),
            mCeiledWidth,
            mCeiledHeight
        );
    }
}