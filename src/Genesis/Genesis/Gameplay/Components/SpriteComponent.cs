using System.Collections.Specialized;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace Genesis.Gameplay.Components;

public struct SpriteComponent(Texture2D spriteSheet, Rectangle sourceRect, float layerDepth = 0.1f, float scale = 1.0f,Vector2 offset = default)
{
    /// <summary>
    /// The spritesheet
    /// </summary>
    public Texture2D SpriteSheet { get; set; } = spriteSheet;

    /// <summary>
    /// The source rectangle within the spritesheet
    /// </summary>
    public Rectangle SourceRect { get; set; } = sourceRect;

    /// <summary>
    /// The center of rotation and scaling (usually the middle)
    /// </summary>
    public Vector2 Origin { get; set; } = new Vector2((float)sourceRect.Width / 2, (float)sourceRect.Height / 2);

    /// <summary>
    /// The "depth" of the texture , is added on the calculated layerdepth in drawsystem , standard 0.1f
    /// </summary>
    public float LayerDepth { get; set; } = layerDepth;

    /// <summary>
    /// Per-sprite scale (used by DrawSystem).
    /// </summary>
    public float mScale = scale;
    
    /// <summary>
    /// Color for Tinting (used by DrawSystem).
    /// </summary>
    public Color mColor = Color.White;
    
    //offset um den es versetzt ysorting berechnet
    public Vector2 mOffset =  offset;
    
    /// <summary>
    /// Rotation in Radiants
    /// </summary>
    public float Rotation { get; set; } = 0f;
}