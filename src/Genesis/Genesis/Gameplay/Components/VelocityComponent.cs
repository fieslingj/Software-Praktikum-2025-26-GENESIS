using System;
using Microsoft.Xna.Framework;

namespace Genesis.Gameplay.Components;

/// <summary>
/// The current movement direction of the entity.
/// </summary>
[Serializable]
public struct VelocityComponent(Vector2 direction, float speed = 1.0f)
{
    public Vector2 Direction { get; set; } = direction;
    public float Value { get; set; } = speed;
    public float BaseSpeed { get; init; } = speed;
    
}