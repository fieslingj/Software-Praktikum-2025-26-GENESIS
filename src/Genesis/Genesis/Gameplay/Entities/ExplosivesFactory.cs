using System.Collections.Generic;
using System.Linq;
using Arch.Core;
using Genesis.Architecture.Audio;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Definitions;
using Genesis.Gameplay.Navigation;
using Genesis.Gameplay.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace Genesis.Gameplay.Entities;

public class ExplosivesFactory(ContentManager content, AudioService sounds)
{
    private readonly EffectFactory mEffectFactory = new(content);
    private readonly Texture2D mStunGrenadeTexture = content.Load<Texture2D>("Sprites/Weapons/StunGrenade");
    private readonly Texture2D mRemoteExplosiveTexture = content.Load<Texture2D>("Sprites/Weapons/RemoteExplosive");
    private const float TargetWidth = 15f;

    private const string ThrowSoundPath = "Sounds/Attack/ConfirmTestSound";
    private const string ExplosionSoundPath = "Sounds/Effects/ExplosionSound";

    public void CreateStunGrenadeProjectile(World world, Vector2 sourcePosition, Vector2 targetPosition, Entity shooter)
    {
        var def = ItemDefinitions.Get(ItemType.StunGrenade);

        var texture = mStunGrenadeTexture;
        var scale = TargetWidth / texture.Width;
        var direction = targetPosition - sourcePosition;
        if (direction != Vector2.Zero) { direction.Normalize(); }

        var speed = def.ProjectileSpeed > 0 ? def.ProjectileSpeed : 1f;
        var flightTime = def.AttackRange / speed;
        world.Create(
            new PositionComponent(sourcePosition),
            new ProjectileComponent(def.Damage, shooter, flightTime, ThrowSoundPath, true, ProjectileType.Grenade, world.IsEnemy(shooter)),
            new SpriteComponent(texture, texture.Bounds, 0.1f, scale),
            new VelocityComponent(direction, speed),
            new HitBoxComponent(new Vector2(texture.Width * scale, texture.Height * scale), Vector2.Zero)
        );
    }

    public void CreateRemoteExplosiveFlying(World world, Vector2 sourcePosition, Vector2 targetPosition, Entity shooter)
    {
        var def = ItemDefinitions.Get(ItemType.RemoteExplosive);

        sounds.PlaySfx(ThrowSoundPath);

        var texture = mRemoteExplosiveTexture;
        var scale = TargetWidth / texture.Width;
        var direction = targetPosition - sourcePosition;
        if (direction != Vector2.Zero) { direction.Normalize(); }

        var speed = def.ProjectileSpeed > 0 ? def.ProjectileSpeed : 1f;
        var flightTime = def.AttackRange / speed;

        world.Create(
            new PositionComponent(sourcePosition),
            new ProjectileComponent(def.Damage, shooter, flightTime, ThrowSoundPath, true, ProjectileType.RemoteExplosive, world.IsEnemy(shooter)),
            new SpriteComponent(texture, texture.Bounds, 0.1f, scale),
            new VelocityComponent(direction, speed),
            new HitBoxComponent(new Vector2(texture.Width * scale, texture.Height * scale), Vector2.Zero),
            new RemoteExplosiveComponent()
        );
    }

    public void CreateGrenadeTimer(World world, Vector2 position, Entity shooter)
    {
        var def = ItemDefinitions.Get(ItemType.StunGrenade);
        var scale = TargetWidth / mStunGrenadeTexture.Width;

        List<Entity> effects = [];

        // Explosion animation
        var animPos = position - new Vector2(0, 50f);
        var mainAnim = mEffectFactory.Create(world, animPos, EffectType.ExplosionAnimation);
        effects.Add(mainAnim);

        // Smoke wave
        var smokeWave = mEffectFactory.Create(world, position, EffectType.SmokeWaveAnimationSmall);
        effects.Add(smokeWave);

        List<StatusType> statusList = [];
        if (def.AoeStatusEffect != StatusType.None) { statusList.Add(def.AoeStatusEffect); }

        world.Create(
            new VelocityComponent(Vector2.Zero),
            new PositionComponent(position),
            new SpriteComponent(mStunGrenadeTexture, mStunGrenadeTexture.Bounds, 0.1f, scale),
            new AreaOfEffectComponent(def.AoeRange, def.AoeDamage, effects, statusList, ExplosionSoundPath),
            new ProjectileComponent(0, shooter, def.ProjectileLifeTime, null, false, ProjectileType.Grenade, world.IsEnemy(shooter))
        );
    }

    public void CreateRemoteExplosivePlaced(World world, Vector2 position)
    {
        var texture = mRemoteExplosiveTexture;
        var scale = TargetWidth / texture.Width;

        world.Create(
            new PositionComponent(position),
            new SpriteComponent(texture, texture.Bounds, 0.1f, scale),
            new RemoteExplosiveComponent()
        );

        var gridMap = world.GetResource<GridMap>();
        if (gridMap != null)
        {
            float avoidanceRadius = 32f;
            Vector2 areaSize = new Vector2(avoidanceRadius * 2);
            // They have to be the same values as in AddDynamicWeight!
            gridMap.AddDynamicWeight(position, areaSize, 250);
        }
    }

    public void DetonateRemoteExplosive(World world, Vector2 position)
    {
        var gridMap = world.GetResource<GridMap>();
        if (gridMap != null)
        {
            float avoidanceRadius = 32f;
            Vector2 areaSize = new Vector2(avoidanceRadius * 2);
            // They have to be the same values as in RemoveDynamicWeight!
            gridMap.RemoveDynamicWeight(position, areaSize, 250);
        }

        var def = ItemDefinitions.Get(ItemType.RemoteExplosive);
        sounds.PlaySfx(ExplosionSoundPath);
        List<Entity> effects = [];

        // Explosion animation
        var animPos = position - new Vector2(0, 50f);
        var mainAnim = mEffectFactory.Create(world, animPos, EffectType.ExplosionAnimation);
        effects.Add(mainAnim);

        // Smoke wave
        var smokeWave = mEffectFactory.Create(world, position, EffectType.SmokeWaveAnimationSmall);
        effects.Add(smokeWave);

        // Activate the effects
        foreach (var effect in effects.Where(world.IsAlive))
        {
            world.Get<EffectComponent>(effect).Active = true;
            world.Get<LifeTimeComponent>(effect).Active = true;
        }

        var player = world.GetFirstEntity(new QueryDescription().WithAll<PlayerTagComponent>());

        List<StatusType> statusList = [];
        if (def.AoeStatusEffect != StatusType.None) { statusList.Add(def.AoeStatusEffect); }

        // Create the AOE entity
        world.Create(
            new PositionComponent(position),
            new AreaOfEffectComponent(def.AoeRange, def.AoeDamage, effects, statusList),
            new ProjectileComponent(0, player, 0.1f, null, false, ProjectileType.RemoteExplosive, false)
        );
    }
}