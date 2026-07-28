using System;
using Arch.Core;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Components.Visuals;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
namespace Genesis.Gameplay.Entities;

/// <summary>
/// Factory that creates Effects consisting of visual and auditive Feedback
/// </summary>
/// <param name="content">zum laden von sprite</param>
public class EffectFactory(ContentManager content)
{
    public Entity Create(World world, Vector2 position, EffectType effectType, bool isFromTiled = false)
    {
        return effectType switch
        {
            EffectType.ExplosionAnimation =>
                CreateExplosionAnimation(world, position, effectType, isFromTiled),

            EffectType.SmokeWaveAnimationSmall =>
                CreateSmokeWaveAnimation(world, position, effectType, isFromTiled),

            EffectType.LeakingChemicalTank =>
                CreateLeakingChemicalTank(world, position, effectType, isFromTiled),

            _ => CreateSmokeWaveAnimation(world, position, effectType, isFromTiled)
        };
    }

    private Entity Create(
        World world,
        Vector2 position,
        EffectType effectType,
        Rectangle sourceRect,
        int frameCount,
        float frameDurationMilliseconds,
        float lifeTime
    )
    {
        var effectTexture = content.Load<Texture2D>($"Sprites/Effects/{effectType}");

        return world.Create(
            new PositionComponent(position),
            new EffectComponent(effectType),
            new SimpleAnimationComponent(sourceRect.Width, sourceRect.Height, frameCount, 12, frameDurationMilliseconds, false),
            new LifeTimeComponent(lifeTime),
            new SpriteComponent(effectTexture, sourceRect, 0.1f)
        );
    }

    private Entity CreateExplosionAnimation(World world, Vector2 position, EffectType effectType, bool isFromTiled = false)
    {
        const int frameCount = 12;
        const float frameDurationMilliseconds = 50;
        const float lifeTime = frameDurationMilliseconds * frameCount / 1000;
        const int textureWidth = 128;
        const int textureHeight = 128;

        var sourceRect = new Rectangle(0, 0, textureWidth, textureHeight);

        // Offset position when loaded from Tiled
        if (isFromTiled) { position -= new Vector2(-textureWidth / 2f, textureHeight / 2f); }

        return Create(world, position, effectType, sourceRect, frameCount, frameDurationMilliseconds,
            lifeTime);
    }

    private Entity CreateSmokeWaveAnimation(World world, Vector2 position, EffectType effectType, bool isFromTiled = false)
    {
        const int frameCount = 72;
        const float frameDurationMilliseconds = 20;
        const float lifeTime = frameDurationMilliseconds * frameCount / 1000;
        const int textureWidth = 107;
        const int textureHeight = 107;

        var sourceRect = new Rectangle(0, 0, textureWidth, textureHeight);

        if (isFromTiled) { position -= new Vector2(-textureWidth / 2f, textureHeight / 2f); }

        return Create(world, position, effectType, sourceRect, frameCount, frameDurationMilliseconds,
            lifeTime);
    }

    private Entity CreateLeakingChemicalTank(World world, Vector2 position, EffectType effectType, bool isFromTiled = false)
    {
        const int frameCount = 16;
        const float frameDurationMilliseconds = 200;
        const float lifeTime = frameDurationMilliseconds * frameCount / 1000;
        const int textureWidth = 64;
        const int textureHeight = 64;

        var sourceRect = new Rectangle(0, 0, textureWidth, textureHeight);

        if (isFromTiled) { position -= new Vector2(-textureWidth, textureHeight / 2f); }

        return Create(world, position, effectType, sourceRect, frameCount, frameDurationMilliseconds,
            lifeTime);
    }

    public Entity CreateCompanionHeart(World world, Entity owner)
    {
        var texture = content.Load<Texture2D>("Sprites/Effects/heart");

        return world.Create(
            new PositionComponent(Vector2.Zero), // Position is set by system
            new CompanionHeartComponent(owner),
            new SpriteComponent(texture, new Rectangle(0, 0, 15, 15), 0.16f, 1.0f),
            new SimpleAnimationComponent(15, 15, 2, 2, 500f, true),
            new IgnoreCullingComponent(),
            new IsVisibleComponent()
        );
    }
}