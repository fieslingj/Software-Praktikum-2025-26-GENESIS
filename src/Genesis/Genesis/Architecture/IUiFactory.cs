using System;
using Arch.Core;
using Genesis.Architecture.Audio;
using Genesis.Gameplay.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Genesis.Architecture;

public interface IUiFactory
{
    void Initialize(GameServices services, AudioService audioService);
    Arch.Core.Entity CreateButton(World world, Vector2 position, string text, Action onClick, Rectangle? bounds = null);

    Arch.Core.Entity CreateButtonWithSize(World world, Vector2 position, string text, Action onClick, int width, int height, Point? padding = null);

    Entity CreateButtonWithSprite(World world,
        Vector2 position,
        string text,
        Action onClick,
        Rectangle targetPixels,
        Point? padding = null,
        SpriteFont font = null);

    Arch.Core.Entity CreateText(Arch.Core.World world, Vector2 position, string text, Microsoft.Xna.Framework.Graphics.SpriteFont font,
                                Microsoft.Xna.Framework.Color color, TextAlignment alignment);

    Arch.Core.Entity MarkAsStaticUi(World world, Entity entity);

    Arch.Core.Entity CreateImage(Arch.Core.World world, Vector2 position, Microsoft.Xna.Framework.Graphics.Texture2D texture,
                             System.Nullable<Microsoft.Xna.Framework.Rectangle> sourceRect = null, float depth = 0.5f);
}