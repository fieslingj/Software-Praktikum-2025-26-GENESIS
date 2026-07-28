using System;
using Arch.Core;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Components.Visuals;
using Genesis.Gameplay.Extensions;
using Genesis.Gameplay.Navigation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Tiled;

namespace Genesis.Gameplay.Entities;

public static class ChemicalTankEntity
{
    // Change interaction radius as needed
    private const float InteractionRadius = 64f;

    /// <summary>
    /// Creates a chemical tank entity with position, collider, sprite, hitbox, and chemical tank components.
    /// </summary>
    public static Entity Create(World world, TiledMapObject mapObject, Texture2D spriteSheet)
    {
        // Sprite and animation setup
        const int frameWidth = 32;
        const int frameHeight = 64;
        const int frameCount = 6;

        var position = new Vector2(
            mapObject.Position.X + frameWidth / 2f,
            mapObject.Position.Y + frameHeight / 2f
        );

        var sourceRect = new Rectangle(0, 0, frameWidth, frameHeight);

        var yoffset = new Vector2(0, 16);
        
        var spriteComponent = new SpriteComponent(
            spriteSheet,
            sourceRect,
            0.1f,
            scale: 1.0f,
            yoffset
        );

        var animationComponent = new SimpleAnimationComponent(
            frameWidth: frameWidth,
            frameHeight: frameHeight,
            frameCount: frameCount,
            framesPerRow: frameCount,
            frameDuration: 100f,
            isLooping: false
        );

        animationComponent.CurrentFrame = 0;
        animationComponent.FrameTimer = 0f;
        animationComponent.IsFinished = true;

        var hitboxOffsetX = 12f;
        var hitboxOffsetY = 20f;

        var hitboxSize = new Vector2(sourceRect.Width - hitboxOffsetX, sourceRect.Height - hitboxOffsetY);
        var hitboxOffset = Vector2.Zero;
        var hitBox = new HitBoxComponent(hitboxSize, hitboxOffset);

        // Collider is only a third of the hitbox height, positioned at the bottom
        var colliderHeight = hitboxSize.Y / 3f;
        var colliderSize = new Vector2(hitboxSize.X, colliderHeight);

        var colliderOffsetY = (hitboxSize.Y / 2f) - (colliderHeight / 2f);
        var colliderOffset = new Vector2(0f, colliderOffsetY);

        var collider = new ColliderComponent(colliderSize, colliderOffset);

        var interactable = new InteractableComponent(
            InteractionRadius,
            InteractionType.ChemicalTank
        );

        var healthComponent = new HealthComponent(20, 20);

        var entity = world.Create(
            new PositionComponent(position),
            animationComponent,
            spriteComponent,
            new ChemicalTankComponent(TankState.Intact),
            collider,
            hitBox,
            interactable,
            healthComponent
        );
        
        var gridMap = world.GetResource<GridMap>();
        if (gridMap == null)
        {
            Console.WriteLine("[ChemicalTankEntity] Warning: GridMap resource is missing!");
        }
        gridMap.MarkColliderAsUnwalkable(position, collider);
        
        return entity;
    }
}