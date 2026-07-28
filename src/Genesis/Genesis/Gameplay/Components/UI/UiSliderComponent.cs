using System;
using Microsoft.Xna.Framework;

namespace Genesis.Gameplay.Components.UI;

public struct UiSliderComponent(Rectangle bounds, float min, float max, float initial, Action<float> onSet)
{
    public float Min { get; } = min;
    public float Max { get; } = max;
    public float Value { get; set; } = initial;

    public Rectangle Bounds { get; } = bounds;
    public Action<float> OnSet { get; } = onSet;
    public UiSliderState State { get; set; } = UiSliderState.Idle;
}

public enum UiSliderState
{
    Idle,
    Hover,
    Dragging,
}