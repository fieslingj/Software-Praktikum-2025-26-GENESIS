using System;
using System.Collections.Generic;
using Arch.Core;
using Genesis.Architecture.Audio;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Components.Visuals;
using Genesis.Gameplay.Definitions;
using Genesis.Gameplay.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace Genesis.Gameplay.Entities;

public class ProjectileEntity(ContentManager content, AudioService audio)
{
    private readonly Texture2D mBulletSprite = content.Load<Texture2D>("Sprites/Weapons/Projectile_zugeschnitten");

    private const string ImpactSoundPath = "";
    private const string BulletPath = "Sprites/Weapons/Projectile_zugeschnitten";

    /// <summary>
    /// Creates a ´projectile entity with position, direction, speed, damage, lifetime, sprite and owner
    /// </summary>
    public Entity Create(
        World world,
        Vector2 position,
        Vector2 direction,
        float speed,
        float damage,
        float lifeTimeSeconds,
        Entity owner,
        string weaponSoundPath,
        string projectileSpritePath = BulletPath,
        float scale = 0.01f,
        bool nahkampf = false,
        bool destroyOnHit = true,
        Vector2? overrideSize = null,
        int framewidth = 0,
        int frameheight = 0,
        float frameduration = 0,
        int framecount = 0)
    {
        // Play sound if path is provided.
        PlayWeaponSound(weaponSoundPath);

        // Prepare movement and physics data.
        Vector2 normalizedDir = GetNormalizedDirection(direction, speed);
        Vector2 colliderSize = DetermineColliderSize(overrideSize, scale);
        
        // Calculate Rotation
        float rotation = (float)Math.Atan2(normalizedDir.Y, normalizedDir.X);

        // Create base components.
        var posComp = new PositionComponent(position);
        var velComp = new VelocityComponent(normalizedDir, speed);
        var projComp = new ProjectileComponent(damage, owner, lifeTimeSeconds, ImpactSoundPath, destroyOnHit, ProjectileType.Bullet, world.IsEnemy(owner), nahkampf);
        var hitBoxComp = new HitBoxComponent(colliderSize, Vector2.Zero);

        // Melee has no sprite
        if (nahkampf)
        {
            return world.Create(posComp, velComp, projComp, hitBoxComp);
        }

        // Handle visuals (Sprite and Animation)
        var sprite = LoadProjectileSprite(projectileSpritePath);
        var sourceRect = GetSourceRectangle(sprite, framewidth, frameheight, frameduration);
        var spriteComponent = new SpriteComponent(sprite, sourceRect, 0.1f, scale)
        { 
            Rotation = rotation 
        };

        if (framewidth > 0)
        {
            var animcomponent =
                new SimpleAnimationComponent(framewidth, frameheight, framecount, framecount, frameduration, false);
            return world.Create(
                posComp,
                velComp,
                spriteComponent,
                projComp,
                hitBoxComp,
                animcomponent
            );
        }

        return world.Create(
            posComp,
            velComp,
            spriteComponent,
            projComp,
            hitBoxComp
        );
    }

    /// <summary>
    /// Plays the weapon sound effect if a valid path is provided.
    /// </summary>
    private void PlayWeaponSound(string path)
    {
        if (!string.IsNullOrEmpty(path)) { audio.PlaySfx(path); }
    }

    /// <summary>
    /// Normalizes the direction vector if the projectile has movement.
    /// </summary>
    private Vector2 GetNormalizedDirection(Vector2 direction, float speed)
    {
        if (direction != Vector2.Zero && speed > 0)
        {
            direction.Normalize();
        }
        return direction;
    }

    /// <summary>
    /// Calculates the size of the hitbox based on an override or the default bullet sprite scale.
    /// </summary>
    private Vector2 DetermineColliderSize(Vector2? overrideSize, float scale)
    {
        if (overrideSize.HasValue)
        {
            return overrideSize.Value;
        }
        // Fallback: Take the bullet size. But this should not happen.
        var refRect = mBulletSprite.Bounds;
        return new Vector2(refRect.Width * scale, refRect.Height * scale);
    }

    /// <summary>
    /// Loads the texture for the projectile or returns the default bullet sprite.
    /// </summary>
    public Texture2D LoadProjectileSprite(string path)
    {
        if (!string.IsNullOrEmpty(path) && path != BulletPath)
        {
            return content.Load<Texture2D>(path);
        }
        return mBulletSprite;
    }

    /// <summary>
    /// Determines the source rectangle for drawing, considering animation frames.
    /// </summary>
    private Rectangle GetSourceRectangle(Texture2D sprite, int width, int height, float duration)
    {
        if (width > 0 && duration > 0)
        {
            return new Rectangle(new Point(0, 0), new Point(width, height));
        }
        return sprite.Bounds;
    }

    public Entity CreateFlyingAoe(
        World world,
        Vector2 position,
        Vector2 direction,
        float speed,
        float damageOnHit,
        float lifeTimeSeconds,
        Entity owner,
        float aoeRadius,
        float aoeDamage,
        string projectileSpritePath,
        List<StatusType> statusEffectList,
        string weaponSoundPath,
        float scale = 1f,
        bool nahkampf = false,
        bool rotated = false,
        bool destroyOnHit = true,
        Vector2? overrideSize = null)
    {
        // Normalize direction if already set and if projectile moves
        var dir = direction;
        if (dir != Vector2.Zero && speed > 0)
        {
            dir.Normalize();
        }

        Texture2D sprite = content.Load<Texture2D>(projectileSpritePath);

        if (!string.IsNullOrEmpty(weaponSoundPath))
        {
            audio.PlaySfx(weaponSoundPath);
        }

        var sourceRect = sprite.Bounds;
        var finalScale = scale;
        Vector2 colliderSize;

        // Size
        if (overrideSize.HasValue)
        {
            finalScale = overrideSize.Value.X / sourceRect.Width;
            colliderSize = overrideSize.Value;
        }
        else
        {
            colliderSize = new Vector2(sourceRect.Width * scale, sourceRect.Height * scale);
        }

        var spriteComponent = new SpriteComponent(sprite, sourceRect, 0.1f, finalScale);

        var entity = world.Create(
            new PositionComponent(position),
            new VelocityComponent(dir, speed),
            spriteComponent,
            new ProjectileComponent(damageOnHit, owner, lifeTimeSeconds, ImpactSoundPath, destroyOnHit, ProjectileType.Bullet, world.IsEnemy(owner)),
            new HitBoxComponent(colliderSize, Vector2.Zero),
            new AreaOfEffectComponent(aoeRadius, aoeDamage, [], statusEffectList)
        );

        return entity;
    }
}