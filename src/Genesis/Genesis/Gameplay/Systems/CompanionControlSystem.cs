using System;
using System.Collections.Generic;
using Arch.Core;
using Genesis.Architecture;
using Genesis.Architecture.Audio;
using Genesis.Architecture.ECS;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Entities;
using Genesis.Gameplay.Extensions;
using Genesis.Gameplay.Navigation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace Genesis.Gameplay.Systems;

public class CompanionControlSystem(FactoryService factoryService, AudioService audio, SpatialHash spatialHash) : IUpdateSystem
{
    public bool IsTechDemoMode { get; set; } = false;
    private readonly List<(Rectangle Bounds, Vector2 Position)> mStaticObstacles = [];

    private static readonly QueryDescription sPlayerQuery = new QueryDescription()
        .WithAll<PlayerTagComponent, PositionComponent, ColliderComponent>();

    private static readonly QueryDescription sCompanionQuery = new QueryDescription()
        .WithAll<CompanionComponent, PositionComponent, VelocityComponent, ColliderComponent, StateComponent, LoadoutComponent, HitBoxComponent>();

    private static readonly QueryDescription sStaticColliderQuery = new QueryDescription()
        .WithAll<PositionComponent, ColliderComponent>()
        .WithNone<VelocityComponent>();

    private static readonly QueryDescription sAllCompanionsQuery = new QueryDescription()
        .WithAll<CompanionComponent, PositionComponent, StateComponent>();

    private static readonly QueryDescription sCommanderQuery = new QueryDescription().WithAll<CompanionSelectionComponent>();
    private static readonly QueryDescription sCompanionInteractQuery = new QueryDescription().WithAll<CompanionComponent, PositionComponent, HitBoxComponent>();
    private static readonly QueryDescription sEnemyInteractQuery = new QueryDescription().WithAll<EnemyComponent, PositionComponent, HitBoxComponent>();
    private static readonly QueryDescription sTrapInteractQuery = new QueryDescription().WithAll<TrapComponent, PositionComponent, SpriteComponent>();

    public void Update(World world, GameTime gameTime)
    {
        var gridMap = world.GetResource<GridMap>();
        if (gridMap == null)
        {
            Console.WriteLine("[EnemyControlSystem] Warning: GridMap resource is missing!");
        }
        var flowField = world.GetResource<FlowField>();
        if (flowField == null)
        {
            Console.WriteLine("[EnemyControlSystem] Warning: FlowField resource is missing!");
        }

        // Update obstacle cache
        mStaticObstacles.Clear();
        world.Query(in sStaticColliderQuery, (ref PositionComponent pos, ref ColliderComponent col) =>
        {
            mStaticObstacles.Add((col.GetAabb(pos.Value), pos.Value));
        });

        // Handle deaths
        world.Query(in sAllCompanionsQuery, (Entity entity, ref PositionComponent pos, ref StateComponent state) =>
        {
            if (state.Current == ActorState.Dead)
            {
                HandleCompanionDeath(entity, pos.Value, world, gridMap);
            }
        });

        // Player information
        var playerEntity = world.GetFirstEntity(in sPlayerQuery);
        var playerPos = Vector2.Zero;
        float playerRadius = 0;

        if (playerEntity != Entity.Null)
        {
            playerPos = world.Get<PositionComponent>(playerEntity).Value;
            playerRadius = world.Get<ColliderComponent>(playerEntity).Radius;
        }

        world.Query(in sCompanionQuery, (
            Entity entity,
            ref CompanionComponent companion,
            ref PositionComponent pos,
            ref VelocityComponent vel,
            ref ColliderComponent collider,
            ref StateComponent state,
            ref LoadoutComponent loadout,
            ref AmmoComponent ammo) =>
        {
            var currentPos = pos.Value;
            // Reset Speed & State defaults
            vel.Value = vel.BaseSpeed;

            var (localSeparation, neighborCount) = NavigationHelper.CalculateSeparationForce(spatialHash, entity, false, pos.Value, collider);

            // Check if companion has a valid target, otherwise remove the target component
            var targetEntity = companion.TargetEntity;
            var hasValidTarget = false;
            var isTrap = false;

            if (targetEntity != Entity.Null && world.IsAlive(targetEntity))
            {
                if (world.Has<PositionComponent>(targetEntity))
                {
                    if (world.Has<HealthComponent>(targetEntity)) { hasValidTarget = true; }
                    else if (world.Has<TrapComponent>(targetEntity) && world.Get<TrapComponent>(targetEntity).IsActive)
                    {
                        hasValidTarget = true;
                        isTrap = true;
                    }
                }
            }

            if (!hasValidTarget)
            { 
                companion.TargetEntity = Entity.Null; 
            }

            if (hasValidTarget)
            {
                var targetPos = world.Get<PositionComponent>(targetEntity).Value;
                var distToTarget = Vector2.Distance(pos.Value, targetPos);

                if (isTrap)
                {
                    var interactionRange = world.Get<InteractableComponent>(targetEntity).Radius;
                    if (distToTarget <= (interactionRange + collider.Radius))
                    {
                        vel.Direction = Vector2.Zero;
                        InteractionSystem.DefuseTrap(world, targetEntity);
                        companion.TargetEntity = Entity.Null;
                    }
                    else
                    {
                        NavigationHelper.MoveToTarget(world, entity, currentPos, targetPos, gridMap, ref vel, ref state, localSeparation);
                    }
                    return;
                }
                
                var aimTargetPos = world.GetCenter(targetEntity);
                var aimSourcePos = world.GetCenter(entity);

                // Choose Weapon
                var canUseRanged = loadout.HasRanged && (!loadout.Ranged.UsesAmmo || ammo.Current > 0);
                var weaponToUse = canUseRanged ? loadout.Ranged : (loadout.HasMelee ? loadout.Melee : null);
                var attackRange = weaponToUse?.AttackRange ?? 40f;

                // Check: in range, line of sight?
                var inRange = distToTarget <= attackRange * 0.9f;
                var hasSight = CombatExtensions.HasLineOfSight(aimSourcePos, aimTargetPos, mStaticObstacles);
                if (inRange && hasSight && weaponToUse != null)
                {
                    // Stop to attack, but apply separation force
                    vel.Direction = Vector2.Zero;
                    if (localSeparation != Vector2.Zero)
                    {
                        vel.Direction = Vector2.Normalize(localSeparation) * 0.2f;
                    }

                    if (state.Current != ActorState.Attacking && state.Current != ActorState.Hit) { state.Current = ActorState.Idle; }

                    if (world.UseWeapon(entity, weaponToUse, aimTargetPos - aimSourcePos, factoryService, audio))
                    {
                        if (state.Current != ActorState.Hit)
                        {
                            state.Previous = state.Current;
                            state.Current = ActorState.Attacking;
                        }
                    }
                    return;
                }
                // Else, move to the target
                NavigationHelper.MoveToTarget(world, entity, pos.Value, targetPos, gridMap, ref vel, ref state, localSeparation);
            }

            // If no target, move to the player
            else if (playerEntity != Entity.Null)
            {
                var baseStopDist = playerRadius + collider.Radius + 50f;
                var dynamicStopDist = baseStopDist + (neighborCount * 20f);

                // CROWD CONTROL: If many neighbors: don't move by yourself
                if (neighborCount > 2)
                {
                    vel.Direction = localSeparation * 0.5f;
                    state.Current = vel.Direction.LengthSquared() > 0.1f ? ActorState.Walking : ActorState.Idle;
                }
                else
                {
                    NavigationHelper.MoveToPlayer(
                        world,
                        entity,
                        pos.Value,
                        playerPos,
                        dynamicStopDist,
                        gridMap,
                        flowField,
                        IsTechDemoMode,
                        ref vel,
                        ref state,
                        localSeparation
                    );
                }
            }
        });
    }

    private void HandleCompanionDeath(Entity companion, Vector2 position, World world, GridMap gridMap)
    {
        if (world.Has<MovementSoundComponent>(companion))
        {
            ref var sound = ref world.Get<MovementSoundComponent>(companion);
            if (sound.WalkSoundInstance != null)
            {
                audio.StopSfxInstance(sound.WalkSoundInstance);
                sound.WalkSoundInstance = null;
            }
        }

        // Companions should not drop corpses - just destroy the entity
        world.Destroy(companion);
    }

     // Handle companion commands: Return true if a new command was given
    public static bool HandleCompanionCommand(World world, Vector2 mousePos)
    {
        // Get the player and the selected companion
        var commanderEntity = world.GetFirstEntity(sCommanderQuery);
        if (commanderEntity == Entity.Null) { return false; }
        ref var selection = ref world.Get<CompanionSelectionComponent>(commanderEntity);

        var clickedCompanion = Entity.Null;

        // Was a new companion selected?
        world.Query(in sCompanionInteractQuery, (Entity companion, ref PositionComponent pos, ref HitBoxComponent hitbox) =>
        {
            if (clickedCompanion != Entity.Null) { return; }

            var rect = hitbox.GetBounds(pos.Value);

            var inflateX = 0.25f * rect.Width;
            var inflateY = 0.25f * rect.Height;

            rect.Inflate(inflateX, inflateY);

            if (!rect.Contains(mousePos)) { return; }
            clickedCompanion = companion;
        });

        if (clickedCompanion != Entity.Null)
        {
            world.Get<CompanionComponent>(clickedCompanion).TargetEntity = Entity.Null;
            selection.Companion = clickedCompanion;
            return true;
        }

        if (selection.Companion == Entity.Null || !world.IsAlive(selection.Companion)) { return false; }

        var clickedTarget = Entity.Null;
        world.Query(in sEnemyInteractQuery, (Entity enemy, ref PositionComponent pos, ref HitBoxComponent hitbox) =>
        {
            if (clickedTarget != Entity.Null) { return; }

            var rect = hitbox.GetBounds(pos.Value);
            if (!rect.Contains(mousePos)) { return; }
            clickedTarget = enemy;
        });
        if (clickedTarget == Entity.Null)
        {
            world.Query(in sTrapInteractQuery, (Entity trap, ref PositionComponent pos, ref SpriteComponent sprite) =>
            {
                if (clickedTarget != Entity.Null) { return; }

                var width = sprite.SourceRect.Width * sprite.mScale;
                var height = sprite.SourceRect.Height * sprite.mScale;

                var x = (int)(pos.Value.X - (width / 2f));
                var y = (int)(pos.Value.Y - (height / 2f));

                var rect = new Rectangle(x, y, (int)width, (int)height);;
                rect.Inflate(rect.Width * 0.25f, rect.Height * 0.25f);

                if (!rect.Contains(mousePos)) { return; }
                clickedTarget = trap;
            });
        }

        if (clickedTarget == Entity.Null) { return false; }

        var companion = selection.Companion;
        world.Get<CompanionComponent>(companion).TargetEntity = clickedTarget;

        return true;
    }
}