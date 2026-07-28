using System;
using Microsoft.Xna.Framework;

namespace Genesis.Gameplay.Components;

public struct ButtonComponent(Rectangle bounds, Action onClickAction)
{
    public Rectangle Bounds { get; set; } = bounds;
    public bool IsHovered { get; set; } = false;
    public bool IsPressed { get; set; } = false;
    public Action OnClick { get; } = onClickAction;
}