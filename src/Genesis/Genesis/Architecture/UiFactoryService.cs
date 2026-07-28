#nullable enable
using System;
using Arch.Core;
using Genesis.Architecture.Audio;
using Genesis.Gameplay.Entities;
using Genesis.Gameplay.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Genesis.Architecture;

public class UiFactoryService(AudioService audioService) : IUiFactory
{
    private Texture2D? mButtonTexture;
    private SpriteFont? mButtonFont;
    public static SpriteFont? mStandartFont;
    
    private bool mInit;

    public void Initialize(GameServices services, AudioService audioService)
    {
        if (mInit) {return;}
        mButtonTexture = services.Content.Load<Texture2D>("Sprites/Buttons/ExampleButton");
        mButtonFont = services.Content.Load<SpriteFont>("Fonts/ButtonFont");
        mStandartFont = services.Content.Load<SpriteFont>("Fonts/ButtonFont");
        
        mInit = true;
    }

    public Entity CreateButton(World world, Vector2 position, string text, Action onClick, Rectangle? bounds = null)
    {
        if (!mInit) {throw new InvalidOperationException("UiFactoryService not initialized.");}
        var rect = bounds ?? new Rectangle(0, 0, 200, 50);
        var entity = ButtonEntity.Create(world, position, text, rect, mButtonTexture!, onClick, mButtonFont!, audioService);
        return MarkAsStaticUi(world, entity);
    }

    public Entity CreateButtonWithSize(World world, Vector2 position, string text, Action onClick, int width, int height, Point? padding = null)
    {
        if (!mInit) {throw new InvalidOperationException("UiFactoryService not initialized.");}
        var pad = padding ?? Point.Zero;
        var rect = new Rectangle(0, 0, Math.Max(1, width + pad.X * 2), Math.Max(1, height + pad.Y * 2));
        var entity = ButtonEntity.Create(world, position, text, rect, mButtonTexture!, onClick, mButtonFont!, audioService);
        return MarkAsStaticUi(world, entity);
    }

    /// <summary>
    /// Creates a button with a target size based on the provided sprite rectangle and optional padding for the text (padding created by gemini)
    /// </summary>
    public Entity CreateButtonWithSprite(World world,
        Vector2 position,
        string text,
        Action onClick,
        Rectangle targetPixels,
        Point? padding = null,
        SpriteFont? font = null)
    {
        if (!mInit) {throw new InvalidOperationException("UiFactoryService not initialized.");}
        var pad = padding ?? Point.Zero;

        int targetW = Math.Max(1, targetPixels.Width + pad.X * 2);
        int targetH = Math.Max(1, targetPixels.Height + pad.Y * 2);

        // Create rectangle for button size
        var rect = new Rectangle(0, 0, targetW, targetH);

        font ??= mButtonFont!;
        var entity = ButtonEntity.Create(world, position, text, rect, mButtonTexture!, onClick, font, audioService);
        return MarkAsStaticUi(world, entity);
    }
    public Entity CreateText(World world, Vector2 position, string text, SpriteFont font, Color color, TextAlignment alignment)
    {
        if (!mInit) { throw new InvalidOperationException("UiFactoryService not initialized."); }
        var entity = world.Create();
        world.Add(entity,
            new PositionComponent(position),
            new TextComponent (text, font, color, alignment)
        );
        return MarkAsStaticUi(world, entity);
    }

    /// <summary>
    /// Marks an entity as static UI by adding visibility and culling-ignore tags.
    /// This ensures the entity is always rendered regardless of camera position.
    /// </summary>
    /// <param name="entity">The entity to mark</param>
    /// <returns>The entity itself for method chaining</returns>
    public Entity MarkAsStaticUi(World world, Entity entity)
    {
        if (!world.Has<IsVisibleComponent>(entity))
        {
            world.Add(entity, new IsVisibleComponent());
        }

        if (!world.Has<IgnoreCullingComponent>(entity))
        {
            world.Add(entity, new IgnoreCullingComponent());
        }
        return entity;
    }

    /// <summary>
    /// Creates an image entity at the specified position with the given texture and optional source rectangle.
    /// </summary>
    public Entity CreateImage(World world, Vector2 position, Texture2D texture, Rectangle? sourceRect = null, float depth = 0.5f)
    {
        if (!mInit) { throw new InvalidOperationException("UiFactoryService not initialized."); }

        var entity = world.Create();

        var src = sourceRect ?? texture.Bounds;
        var origin = new Vector2(src.Width / 2f, src.Height / 2f);

        world.Add(entity,
            new PositionComponent(position),
            new SpriteComponent
            {
                SpriteSheet = texture,
                SourceRect = src,
                Origin = origin,
                mScale = 1.0f,
                mColor = Color.White,
                LayerDepth = depth
            }
        );

        return MarkAsStaticUi(world, entity);
    }
}