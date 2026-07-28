using Microsoft.Xna.Framework;

namespace Genesis.Gameplay.Components;

public struct DuckBehaviorComponent(float probability, float duration, float reactionRange = 250f)
{
    public float Probability { get; } = probability;
    public float Duration { get; } = duration;
    public float CooldownTimer { get; set; } = 0f;
    public float ActionTimer { get; set; } = 0f;
    public float ReactionRange { get; } = reactionRange;

    // Cache for original values
    public Vector2 OriginalHitboxSize { get; set; } = Vector2.Zero;
    public Vector2 OriginalHitboxOffset { get; set; } = Vector2.Zero;
    public float OriginalSpriteScale { get; set; } = 1.0f;
}