using System;
using System.Diagnostics;
using Arch.Core;
using Genesis.Architecture.Audio;
using Genesis.Architecture.ECS;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Components.Visuals;
using Genesis.Gameplay.Extensions;
using Genesis.Gameplay.Navigation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Genesis.Gameplay.Systems;

/// <summary>
/// Manages interaction with chemical tanks, including destruction and acid puddle creation.
/// </summary>
public class ChemicalTankSystem(ContentManager content, AudioService audio) : IUpdateSystem
{
    private static readonly QueryDescription sChemicalTankQuery = new QueryDescription()
        .WithAll<ChemicalTankComponent, SimpleAnimationComponent, PositionComponent, SpriteComponent>();

    private const string GlassBreakSoundPath = "Sounds/Effects/LeakingChemicalTank";
    private Texture2D MAcidPuddleTexture { get; } = content.Load<Texture2D>("Sprites/Props/AcidPuddle");

    public void Update(World world, GameTime gameTime)
    {
        world.Query(in sChemicalTankQuery,
            (Entity entity,
                ref ChemicalTankComponent tank,
                ref SimpleAnimationComponent animation,
                ref PositionComponent position,
                ref SpriteComponent sprite) =>
            {
                // Tank is destroyed but animation not finished yet
                if (tank.State == TankState.Destroyed && !animation.IsFinished)
                {
                    return;
                }

                // Tank is destroyed and animation finished, but puddle not created yet
                if (tank.State == TankState.Destroyed && animation.IsFinished && !tank.PuddleCreated)
                {
                    audio.PlaySfx(GlassBreakSoundPath);
                    SetFinalDestroyedFrame(ref sprite, in animation);
                    CreatePuddle(world, in position);
                    tank.PuddleCreated = true;
                }
            });
    }

    private static void SetFinalDestroyedFrame(ref SpriteComponent sprite, in SimpleAnimationComponent animation)
    {
        var lastFrameIndex = animation.FrameCount - 1;

        var frameWidth = animation.FrameWidth;
        var frameHeight = animation.FrameHeight;

        var frameX = (lastFrameIndex % animation.FramesPerRow) * frameWidth;
        var frameY = (lastFrameIndex / animation.FramesPerRow) * frameHeight;

        sprite.SourceRect = new Rectangle(frameX, frameY, frameWidth, frameHeight);
        sprite.Origin = new Vector2(frameWidth / 2f, frameHeight / 2f);
    }

    /// <summary>
    /// Creates an acid puddle entity beneath the destroyed tank.
    /// </summary>
    private void CreatePuddle(World world, in PositionComponent tankPosition)
    {
        if (MAcidPuddleTexture == null)
        {
            Debug.WriteLine("[ChemicalTankSystem] CreatePuddle aborted: AcidPuddleTexture is null");
            return;
        }

        const float verticalOffset = 8f;
        var puddlePosition = new Vector2(
            tankPosition.Value.X,
            tankPosition.Value.Y + verticalOffset
        );

        var puddleOffsetX = 25f;
        var puddleOffsetY = 15f;
        var size = new Vector2(MAcidPuddleTexture.Width - puddleOffsetX, MAcidPuddleTexture.Height - puddleOffsetY);

        var puddleSprite = new SpriteComponent(
            MAcidPuddleTexture,
            MAcidPuddleTexture.Bounds,
            layerDepth: 0.08f,
            scale: 1.0f
        );

        float textureWidth = MAcidPuddleTexture.Width;
        float hazardRadius = textureWidth / 2f;

        var puddleEntity = world.Create(
            new PositionComponent(puddlePosition),
            new ColliderComponent(size),
            new AcidHazardComponent(),
            new TriggerColliderTagComponent(),
            puddleSprite
        );

        // Two additional collider entities to fullfill the puddle's hazard area
        var overlayColliderSize = new Vector2(100f, 35f);

        var puddleOverlayEntity = world.Create(
            new PositionComponent(puddlePosition),
            new AcidHazardComponent(),
            new ColliderComponent(overlayColliderSize),
            new TriggerColliderTagComponent()
        );

        var overlayColliderSize2 = new Vector2(45f, 90f);

        var puddleOverlayEntity2 = world.Create(
            new PositionComponent(puddlePosition),
            new AcidHazardComponent(),
            new ColliderComponent(overlayColliderSize2),
            new TriggerColliderTagComponent()
        );

        var overlayColliderSize3 = new Vector2(90f, 60f);

        var puddleOverlayEntity3 = world.Create(
            new PositionComponent(puddlePosition),
            new AcidHazardComponent(),
            new ColliderComponent(overlayColliderSize3),
            new TriggerColliderTagComponent()
        );

        var gridMap = world.GetResource<GridMap>();
        if (gridMap == null)
        {
            Console.WriteLine("[ChemicalTankSystem] Warning: GridMap resource is missing!");
        }
    }
}