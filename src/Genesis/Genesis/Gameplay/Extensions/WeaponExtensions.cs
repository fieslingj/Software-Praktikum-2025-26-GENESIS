using System;
using Arch.Core;
using Genesis.Architecture;
using Genesis.Architecture.Audio;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Components.Visuals;
using Genesis.Gameplay.Definitions;
using Microsoft.Xna.Framework;

namespace Genesis.Gameplay.Extensions;

public static class WeaponExtensions
{
    /// <summary>
    /// Handles cooldown, ammo and the creation of projectiles.
    /// Returns whether an attack was performed.
    /// </summary>
    public static bool UseWeapon(
        this World world, 
        Entity owner, 
        ItemDefinition weaponDefinition,
        Vector2 targetDirection, 
        FactoryService factory,AudioService audioService)
    {
        // Check Cooldown
        ref var cooldown = ref world.Get<AttackCooldownComponent>(owner);
        if (cooldown.CurrentTime < cooldown.Delay) { return false; }

        // If weapon uses ammo: Return, if no ammo, otherwise reduce ammo
        if (weaponDefinition.UsesAmmo){
            ref var ammoComp = ref world.Get<AmmoComponent>(owner);
            if (ammoComp.Current <= 0) {audioService.PlaySfxInstancelimited("Sounds/Attack/NoAmmoSound");   return false; }
            ammoComp.Current -= 1;
        }
        
        // If special ability: check if unlocked
        if (world.Has<BloodlustTrackerComponent>(owner) && world.Has<MutantTypeComponent>(owner)){
            ref var bloodlust = ref world.Get<BloodlustTrackerComponent>(owner);
            if (!bloodlust.IsUnlocked)
            {
                var mutant = world.Get<MutantTypeComponent>(owner).Type;
                var specialItem = PlayerDefinitions.Get(mutant).SpecialItem;
                if (specialItem == weaponDefinition.Type)
                {
                    return false;
                }
            }
        }

        // Normalize target direction
        if (targetDirection != Vector2.Zero)
        {
            targetDirection.Normalize();
        }
        else
        {
            targetDirection = Vector2.UnitX;
        }

        // Create projectile
        switch (weaponDefinition.AttackType)
        {
            case ItemAttackType.Melee:
                PerformMeleeAttack(world, factory, owner, targetDirection, weaponDefinition);
                break;
            case ItemAttackType.Ranged:
                PerformRangedAttack(world, factory, owner, targetDirection, weaponDefinition);
                break;
        }

        // Reset cooldown time and set delay according to the cooldown definition of the current weapon
        cooldown.Delay = weaponDefinition.Cooldown;
        cooldown.CurrentTime = 0;
        return true;
    }

    private static void PerformMeleeAttack(World world, FactoryService factoryService, Entity owner, Vector2 direction, ItemDefinition def)
    {
        var ownerCenter = world.GetCenter(owner);
        if (direction != Vector2.Zero)
        {
            direction.Normalize();
        }
        else
        {
            direction = Vector2.UnitX; 
        }

        var finalDamage = GetFinalDamage(world, owner, def);

        // Create invisible projectile for damage detection
        var offsetDistance = def.AttackRange / 2f;
        var spawnPos = ownerCenter + (direction * offsetDistance);
        var hitboxSize = new Vector2(def.AttackRange, def.AttackRange); 
        
        factoryService.MProjectileFactory.Create(
            world: world,
            position: spawnPos,
            direction: direction, 
            speed: 0,
            damage: finalDamage, 
            lifeTimeSeconds: def.ProjectileLifeTime,
            owner: owner,
            weaponSoundPath: def.WeaponUseSoundPath,
            scale: 1.0f,
            nahkampf: true,
            destroyOnHit: true,
            overrideSize: hitboxSize
        );

        // Spawn animated melee slash effect
        float rotation = (float)Math.Atan2(direction.Y, direction.X);
        var slashTexture = factoryService.MProjectileFactory.LoadProjectileSprite("Sprites/Effects/attack_slash");
        var scale = 8f * def.AttackRange / slashTexture.Width;

        world.Create(
            new PositionComponent(ownerCenter),
            new SpriteComponent(slashTexture, new Rectangle(0, 0, 64, 64), 0.15f, scale) 
            { 
                Rotation = rotation,
                Origin = new Vector2(32, 32) // Rotate around the center of the player
            },
            // Non-looping animation: width, height, frameCount, framesPerRow, frameDuration, looping
            new SimpleAnimationComponent(96, 96, 5, 5, 40f, false),
            new LifeTimeComponent(0.2, true), // Duration: 5 frames * 40ms = 0.2s
            new IsVisibleComponent(),
            new IgnoreCullingComponent()
        );
    }

    private static void PerformRangedAttack(World world, FactoryService factoryService, Entity owner, Vector2 direction, ItemDefinition def)
    {
        var ownerCenter = world.GetCenter(owner);
        if (direction == Vector2.Zero) {return;}

        var finalDamage = GetFinalDamage(world, owner, def);

        switch (def.Type)
        {
            case ItemType.StunGrenade:
            {
                var targetCenter = ownerCenter + (direction * def.AttackRange);
                factoryService.MExplosivesFactory.CreateStunGrenadeProjectile(world, ownerCenter, targetCenter, owner);
                return;
            }
            case ItemType.RemoteExplosive:
            {
                var targetCenter = ownerCenter + (direction * def.AttackRange);
                factoryService.MExplosivesFactory.CreateRemoteExplosiveFlying(world, ownerCenter, targetCenter, owner);
                return;
            }
        }

        var hasAoe = def.AoeRange > 0;
        var calculatedLifeTime = def.AttackRange / def.ProjectileSpeed;

        if (!hasAoe)
        {
            factoryService.MProjectileFactory.Create(
                world: world,
                position: ownerCenter,
                direction: direction,
                speed: def.ProjectileSpeed,
                damage: finalDamage,
                lifeTimeSeconds: calculatedLifeTime,
                owner: owner,
                weaponSoundPath: def.WeaponUseSoundPath,
                projectileSpritePath: def.ProjectileSpritePath,
                scale: def.ProjectileSpriteScale,
                overrideSize:def.HitboxSize,
                framewidth:def.Framewidth,
                frameheight:def.Frameheight,
                framecount:def.Frames,
                frameduration:def.FrameDuration
            );
            return;
        }

        factoryService.MProjectileFactory.CreateFlyingAoe(
            world: world,
            position: ownerCenter,
            direction: direction,
            speed: def.ProjectileSpeed,
            damageOnHit: def.Damage,
            lifeTimeSeconds: calculatedLifeTime,
            owner: owner,
            aoeRadius: def.AoeRange,
            aoeDamage: def.AoeDamage,
            projectileSpritePath: def.ProjectileSpritePath,
            statusEffectList: [def.AoeStatusEffect],
            weaponSoundPath:def.WeaponUseSoundPath,
            scale: 0.5f,
            destroyOnHit: true
        );
    }

    // Calculates the final damage of a weapon based on the owner's enemy type and stats
    private static float GetFinalDamage(World world, Entity owner, ItemDefinition weaponDefinition)
    {
        var baseDamage = weaponDefinition.Damage;

        if (world.Has<EnemyComponent>(owner))
        {
            var enemyType = world.Get<EnemyComponent>(owner).Type;
            var enemyDef = EnemyDefinitions.Get(enemyType);
            if (enemyDef.MeleeWeapon == weaponDefinition.Type)
            {
                return baseDamage * enemyDef.MeleeDamageFactor;
            }
            return baseDamage * enemyDef.RangedDamageFactor;
        }

        return baseDamage;
    }
}