using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Genesis.Gameplay.Components;

/// <summary>
/// Stores the data required for rendering a piece of text on the screen.
/// </summary>
public struct TextComponent(string text, SpriteFont font, Color color, TextAlignment alignment=TextAlignment.MiddleLeft, float layerDepth=1f)
{
    /// <summary> Displayed text  </summary>
    public string Text { get; set; } = text;

    /// <summary> Spritefont </summary>
    public SpriteFont Font { get; } = font;

    /// <summary> Size of font </summary>
    public float FontSize { get; } = font.LineSpacing;

    /// <summary> Color of text </summary>
    public Color Color { get; set; } = color;

    /// <summary>Horizontal alignment of the text relative to the position</summary>
    public TextAlignment Alignment { get; } = alignment;

    public float LayerDepth { get; set; } = layerDepth;
}

public enum TextAlignment
{
    TopLeft,
    TopCenter,
    TopRight,
    MiddleLeft,
    MiddleCenter,
    MiddleRight,
    BottomLeft,
    BottomCenter,
    BottomRight
}