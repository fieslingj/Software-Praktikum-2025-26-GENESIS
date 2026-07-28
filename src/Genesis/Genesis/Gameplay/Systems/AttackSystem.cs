using System;
using System.Collections.Generic;
using Genesis.Architecture;
using Genesis.Architecture.ECS;
using Arch.Core;
using Genesis.Architecture.Audio;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Components.Inventory;
using Genesis.Gameplay.Components.Visuals;
using Genesis.Gameplay.Navigation;
using Genesis.Gameplay.Definitions;
using Microsoft.Xna.Framework;
using Genesis.Gameplay.Extensions;

namespace Genesis.Gameplay.Systems;

public class AttackSystem(FactoryService factoryService, AudioService audio, SpatialHash spatialHash) : IUpdateSystem
{
    private static readonly QueryDescription sMovingProjectileQuery = new QueryDescription()
        .WithAll<ProjectileComponent, PositionComponent, VelocityComponent>();

    private static readonly QueryDescription sAllProjectilesQuery = new QueryDescription()
        .WithAll<ProjectileComponent, PositionComponent>();

    private static readonly QueryDescription sEnemyAndPlayerQuery = new QueryDescription()
        .WithAll<PositionComponent, HealthComponent, HitBoxComponent, HitSoundComponent, StateComponent>();

    private static readonly QueryDescription sPlayerQuery = new QueryDescription()
        .WithAll<PlayerTagComponent, PositionComponent, HitBoxComponent>();

    // Reusable list to avoid frame-by-frame allocations
    private readonly List<SpatialEntry> mNearbyEntities = new(32);

    public void Update(World world, GameTime gameTime)
    {
        Entity player = Entity.Null;
        world.Query(in sPlayerQuery, (Entity playerEntity) => { player = playerEntity; });

        //remove state of being hit after time
        world.Query(in sEnemyAndPlayerQuery, (Entity entity,ref StateComponent state) =>
        {
            ResolveKnockbackState(world,entity,ref state,gameTime);
        });

        // Process active projectiles.
        world.Query(in sMovingProjectileQuery, (Entity projectileEntity, ref PositionComponent position,
            ref ProjectileComponent projectileComponent, ref VelocityComponent veloc) =>
        {
            if (!world.Has<HitBoxComponent>(projectileEntity)) { return; }

            ref var projHitBox = ref world.Get<HitBoxComponent>(projectileEntity);
            var projBounds = projHitBox.GetBounds(position.Value);

            // SPATIAL HASHING: Retrieve only entities in the projectile's vicinity
            mNearbyEntities.Clear();
            spatialHash.GetEntitiesInRect(projBounds, mNearbyEntities);

            foreach (var entry in mNearbyEntities)
            {
                bool isHitbox = (entry.mFlags & SpatialFlags.Hitbox) != 0;
                bool isWall = (entry.mFlags & SpatialFlags.Static) != 0;

                if (!isHitbox && !isWall) { continue; }
                if (!projBounds.Intersects(entry.mAabb)) { continue; }

                var target = entry.mEntity;
                // Skip self, invalid entities, or the shooter
                if (target == projectileEntity || target == projectileComponent.Source) { continue; }
                if (!world.IsAlive(target)) { continue; }
                if(world.Has<ProjectileComponent>(target)) { continue; }

                // 1. Check for HitBox-based targets (Enemies, Players, Tanks, Destructibles)
                if (isHitbox)
                {
                    var collided = HandleTargetCollision(world, projectileEntity, target, position.Value, projectileComponent, veloc.Direction, gameTime);
                    if (collided) { break; }
                }
                // 2. Check for Collider-based targets (Walls, Flipped Tables)
                else if (isWall)
                {
                    if (projectileComponent.Nahkampf)
                    {
                        var dir = veloc.Direction;
                        var pos = position.Value;
                        var wall = entry.mAabb;
                        const float margin = 1.0f;

                        // Current bounds
                        var currentCenter = pos + projHitBox.Offset;
                        var halfSize = projHitBox.Size / 2f;
                        var min = currentCenter - halfSize;
                        var max = currentCenter + halfSize;

                        // Cut edges
                        if (Math.Abs(dir.X) > Math.Abs(dir.Y))
                        {
                            if (dir.X > 0) { max.X = Math.Min(max.X, wall.Left - margin); }
                            else { min.X = Math.Max(min.X, wall.Right + margin); }
                        }
                        else
                        {
                            if (dir.Y > 0) { max.Y = Math.Min(max.Y, wall.Top - margin); }
                            else { min.Y = Math.Max(min.Y, wall.Bottom + margin); }
                        }

                        // New bounds
                        projHitBox.Size = Vector2.Max(Vector2.Zero, max - min);
                        var newCenter = (min + max) / 2f;
                        projHitBox.Offset = newCenter - pos;

                        continue;
                    }
                    
                    // Special Case: Tables (only block if flipped)
                    if (world.Has<TableComponent>(target))
                    {
                        if (world.Get<TableComponent>(target).State != TableState.Flipped) { continue; }
                    }
                    // Special Case: Ignore Triggers and Props for projectiles
                    else if (world.Has<TriggerColliderTagComponent>(target) || world.Has<PropTagComponent>(target))
                    {
                        continue;
                    }

                    PlayOptionalSound(projectileComponent.MissSoundPath);
                    HandleProjectileTransition(world, projectileEntity, position.Value, projectileComponent.Type, projectileComponent.Source, gameTime);
                    break;
                }
            }
        });

        // 2. Handle lifetimes and expiration
        UpdateProjectileLifetimes(world, gameTime, player);
    }


    /// <summary>
    /// Unified collision logic for all destructible or living targets found via Spatial Hash.
    /// </summary>
    private bool HandleTargetCollision(World world, Entity proj, Entity target, Vector2 projPos, ProjectileComponent projComp, Vector2 direction, GameTime gameTime)
    {
        // A. Chemical Tanks
        if (world.Has<ChemicalTankComponent>(target))
        {
            ref var tank = ref world.Get<ChemicalTankComponent>(target);
            ref var tankHealth = ref world.Get<HealthComponent>(target);
            if (tank.State != TankState.Destroyed)
            {
                tankHealth.Current -= projComp.Damage;
                if (tankHealth.Current <= 0)
                {
                    // Reset animation to play destruction
                    ref var animation = ref world.Get<SimpleAnimationComponent>(target);
                    animation.CurrentFrame = 0;
                    animation.FrameTimer = 0f;
                    animation.IsFinished = false;
                    
                    tank.State = TankState.Destroyed;
                    audio.PlaySfx("Sounds/Effects/LeakingChemicalTank");
                    world.Remove<InteractableComponent>(target);
                }
            }
        }
        // B. Living Entities (Enemy/Player/Companion)
        else if (world.Has<HealthComponent>(target))
        {
            if (IsFriendly(world, projComp, target)) { return false; }

            var shieldAttackDir = direction;
            if (projComp.Nahkampf == true)
            {
                shieldAttackDir = world.Get<PositionComponent>(target).Value - world.Get<PositionComponent>(projComp.Source).Value;
                shieldAttackDir.Normalize();
            }
            bool blocked = world.Has<ActiveShieldComponent>(target) && ResolveBlock(world, target, shieldAttackDir);
            
            if (!blocked)
            {
                var mass = 1.0f;
                if (world.Has<MassComponent>(target))
                {
                    mass = world.Get<MassComponent>(target).mValue;
                    if (mass <= 0) { mass = 1.0f; }
                }
                
                //knockback verschiebung bei hit
                if (world.Has<VelocityComponent>(target) && world.Has<StateComponent>(target))
                {
                    var dir = world.Get<VelocityComponent>(proj).Direction;
                    ref var targetVel = ref world.Get<VelocityComponent>(target);
                    targetVel.Direction = dir;
                    
                    var baseKnockbackForce = projComp.Nahkampf ? 0.5f : 1.0f; 
                    var knockbackFactor = baseKnockbackForce / mass;
                    targetVel.Value = 100f * knockbackFactor;
                    
                    ref var state = ref world.Get<StateComponent>(target);
                    state.Current = ActorState.Hit;
                    state.PersistenceTime = 100f / mass;
                }
                
                //effect on hit
                
                
                world.InflictDamage(target, new DamagePayload(projComp.Damage, projComp.Source), world.GetCurrentRunTimeSeconds());
                if (world.Has<HitSoundComponent>(target))
                {
                    PlayOptionalSound(world.Get<HitSoundComponent>(target).SoundPath);
                }
                else { PlayOptionalSound(projComp.MissSoundPath); }
            }
        }
        // C. Generic Objects (Tables with Hitboxes, etc.)
        else
        {
            PlayOptionalSound(projComp.MissSoundPath);
        }

        if (projComp.DestroyOnHit)
        {
            HandleProjectileTransition(world, proj, projPos, projComp.Type, projComp.Source, gameTime);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Updates the remaining lifetime of all active projectiles and triggers expiration logic
    /// such as AOE blasts or entity transitions when lifetime reaches zero.
    /// </summary>
    private void UpdateProjectileLifetimes(World world, GameTime gameTime, Entity player)
    {
        world.Query(in sAllProjectilesQuery, (Entity proj, ref PositionComponent pos, ref ProjectileComponent projComp) =>
        {
            projComp.LifeTimeSeconds -= gameTime.ElapsedGameTime.TotalSeconds;
            if (projComp.LifeTimeSeconds > 0) { return; }

            if (world.Has<AreaOfEffectComponent>(proj))
            {
                FinishAoe(world, proj, pos.Value, player, gameTime);
            }
            else if (projComp.Type == ProjectileType.Grenade || projComp.Type == ProjectileType.RemoteExplosive)
            {
                HandleProjectileTransition(world, proj, pos.Value, projComp.Type, player, gameTime);
            }
            else
            {
                world.Destroy(proj);
            }
        });
    }

    /// <summary>
    /// Determines if a target entity belongs to the same faction as the projectile's source
    /// to prevent friendly fire incidents.
    /// </summary>
    private bool IsFriendly(World world, ProjectileComponent projComp, Entity target)
    {
        bool ownerIsEnemy = projComp.SourceIsEnemy;
        return (ownerIsEnemy && world.IsEnemy(target)) || (!ownerIsEnemy && world.IsFriendly(target));
    }

    /// <summary>
    /// Plays a sound effect via the AudioService.
    /// </summary>
    private void PlayOptionalSound(string path)
    {
        if (!string.IsNullOrEmpty(path)) { audio.PlaySfx(path); }
    }

    /// <summary>
    /// Triggers an Area of Effect (AOE) explosion, dealing damage and applying status effects
    /// to nearby entities using Spatial Hashing for optimized proximity detection.
    /// </summary>
    private void FinishAoe(World world, Entity projectileEntity, Vector2 projectilePos, Entity player, GameTime gameTime)
    {
        var aoeComp = world.Get<AreaOfEffectComponent>(projectileEntity);
        PlayOptionalSound(aoeComp.SoundPath);
        mNearbyEntities.Clear();
        spatialHash.GetNearbyEntities(projectilePos, mNearbyEntities);
        
        // Apply ScreenShake according to damage
        switch (aoeComp.Damage)
        {
            case >= 100:
                world.ShakeLarge();
                break;
            case >= 40:
                world.ShakeMedium();
                break;
            default:
                world.ShakeSmall();
                break;
        }

        float radiusSq = aoeComp.Radius * aoeComp.Radius;
        foreach (var entry in mNearbyEntities)
        {
            if ((entry.mFlags & SpatialFlags.Hitbox) == 0) { continue; }
            if (Vector2.DistanceSquared(projectilePos, entry.mPosition) > radiusSq) { continue; }

            var entity = entry.mEntity;
            if(!world.IsAlive(entity)) { continue; }
            if (!world.Has<HealthComponent>(entity)) { continue; }
            
            if(!world.Has<ProjectileComponent>(projectileEntity)) { return; }
            var owner = world.Get<ProjectileComponent>(projectileEntity).Source;

            if (entity == owner) { continue; }
            
            var payload = new DamagePayload(aoeComp.Damage, owner);
            CombatExtensions.InflictDamage(world, entity, payload, world.GetCurrentRunTimeSeconds());

            if (aoeComp.StatusEffects.Count > 0 && !world.Has<ChemicalTankComponent>(entity))
            {

                if (!world.Has<StatusComponent>(entity)) { world.Add(entity, new StatusComponent([])); }
                ref var targetStatus = ref world.Get<StatusComponent>(entity);
                foreach (var effectType in aoeComp.StatusEffects)
                {
                    var index = targetStatus.Types.FindIndex(x => x.Item1 == effectType);
                    if (index >= 0) { targetStatus.Types[index] = (effectType, 0.0); }
                    else { targetStatus.Types.Add((effectType, 0.0)); }
                }
            }
        }

        foreach (var effect in aoeComp.SpriteEffects)
        {
            if (!world.IsAlive(effect)) { continue; }
            if (world.Has<LifeTimeComponent>(effect)) { world.Get<LifeTimeComponent>(effect).Active = true; }
            if (world.Has<EffectComponent>(effect)) { world.Get<EffectComponent>(effect).Active = true; }
        }
        world.Destroy(projectileEntity);
    }

    /// <summary>
    /// Manages the logic for projectiles that transition into other entities upon
    /// impact or expiration (e.g., a Grenade projectile spawning a Grenade Timer).
    /// </summary>
    private void HandleProjectileTransition(World world, Entity projectileEntity, Vector2 currentPos,
        ProjectileType type, Entity shooter, GameTime gameTime)
    {
        if (world.Has<AreaOfEffectComponent>(projectileEntity))
        {
            FinishAoe(world, projectileEntity, currentPos, shooter, gameTime);
            return;
        }

        switch (type)
        {
            case ProjectileType.Grenade:
                factoryService.MExplosivesFactory.CreateGrenadeTimer(world, currentPos, shooter);
                break;
            
            case ProjectileType.RemoteExplosive:
                factoryService.MExplosivesFactory.CreateRemoteExplosivePlaced(world, currentPos);
                break;
        }

        world.Destroy(projectileEntity);
    }

    public static bool ResolveBlock(World world, Entity playerEntity, Vector2 projectiledir)
    {
        var hitAngle = PlayerFacingSystem.FaceAngle(-projectiledir);
        if (world.Get<FaceComponent>(playerEntity).FaceDirection != hitAngle) { return false; }

        var hotbar = world.Get<HotbarComponent>(playerEntity);
        var itemEntity = hotbar.Slots[hotbar.ActiveSlot];
        if (itemEntity == Entity.Null) { return false; }

        var itemType = world.Get<ItemIdentificationComponent>(itemEntity).mType;
        if (itemType != ItemType.Shield) { return false; }

        ref var durability = ref world.Get<DurabilityComponent>(itemEntity);
        durability.mCurrent -= 1;

        if (durability.mCurrent > 0) { return true; }

        //reset after remove 1 from stack
        durability.mCurrent = durability.mMax;

        world.Remove<ActiveShieldComponent>(playerEntity);
        if (world.Get<ItemStackComponent>(itemEntity).mCount == 1)
        {
            ref var state = ref world.Get<StateComponent>(playerEntity);
            if (state.Current != ActorState.Hit)
            {
                state.Current = ActorState.Idle;
            }
        }
        world.Create(new RemoveItemRequestComponent(itemType));
        return true;
    }

    private void ResolveKnockbackState(World world,Entity entity,ref StateComponent state, GameTime time)
    {
        if (state.Current == ActorState.Hit)
        {
            state.PersistenceTime -= time.ElapsedGameTime.TotalMilliseconds;
            if (state.PersistenceTime <= 0)
            {
                state.Current = ActorState.Idle;
                
                //restore basespeed
                if (world.Has<VelocityComponent>(entity))
                {
                    ref var vel = ref world.Get<VelocityComponent>(entity);
                    vel.Value = vel.BaseSpeed;
                }

            }
        }
    }
}