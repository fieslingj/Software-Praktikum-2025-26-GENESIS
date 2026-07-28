using System;
using System.Collections.Generic;
using System.Linq;
using Arch.Core;
using Genesis.Architecture;
using Genesis.Architecture.Audio;
using Genesis.Architecture.ECS;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Components.Visuals;
using Genesis.Gameplay.Definitions;
using Genesis.Gameplay.Entities;
using Genesis.Gameplay.Extensions;
using Genesis.Gameplay.Navigation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using SpatialHash = Genesis.Gameplay.Navigation.SpatialHash;

namespace Genesis.Gameplay.Systems;

/// <summary>
///  The enemy AI. Controls movement, group behavior and attack logic.
/// </summary>
public class EnemyControlSystem(FactoryService factoryService, ContentManager content, AudioService audio, SpatialHash spatialHash, RandomService rng) : IUpdateSystem, IDisposable
{
    private readonly List<SpatialEntry> mNearbyBuffer = new(64);
    private readonly FlowFieldService mFlowFieldService = new();
    public bool IsTechDemoMode { get; set; } = false;
    private World mWorld;
    private Vector2 mLastTargetPosition = new(-999, -999);
    private int mLastGridVersion = 0;
    private const float UpdateThreshold = 20f;
    private const float MaxVisionRange = 1200f;
    private const int TargetUpdateInterval = 30;

    private static readonly QueryDescription sEnemyQueryDesc = new QueryDescription()
        .WithAll<EnemyComponent, PositionComponent, VelocityComponent, StateComponent, StatusComponent, HealthComponent,
            LoadoutComponent, AttackCooldownComponent, AmmoComponent, ColliderComponent, HitBoxComponent>();

    private static readonly QueryDescription sPlayerQueryDesc = new QueryDescription()
        .WithAll<PlayerTagComponent, PositionComponent, HealthComponent>();

    private static readonly QueryDescription sStaticColliderQuery = new QueryDescription()
        .WithAll<PositionComponent, ColliderComponent>()
        .WithNone<VelocityComponent, AcidHazardComponent>();

    private static readonly QueryDescription sDeadEnemiesQuery = new QueryDescription()
        .WithAll<StateComponent, PositionComponent, EnemyComponent>();

    private static readonly QueryDescription sDuckingQuery = new QueryDescription()
        .WithAll<EnemyComponent, DuckBehaviorComponent, StateComponent, HitBoxComponent, SpriteComponent, PositionComponent>()
        .WithNone<DeathStateComponent>();

    private static readonly QueryDescription sProjectileQuery = new QueryDescription()
        .WithAll<ProjectileComponent, PositionComponent>();

    private static readonly QueryDescription sTankQuery = new QueryDescription()
        .WithAll<ChemicalTankComponent, PositionComponent, InteractableComponent>();

    private static readonly QueryDescription sTableQuery = new QueryDescription()
        .WithAll<TableComponent, PositionComponent, InteractableComponent>();

    //to query all covers so enemys dont cover in same position
    private static readonly QueryDescription sCoverQuery = new QueryDescription()
        .WithAll<CoverBehaviorComponent>();

    private readonly List<(Rectangle Bounds, Vector2 Position)> mStaticObstacles = new();
    private readonly List<Vector2> mOccupiedCoverSpots = new(32);
    private readonly List<(Entity Tank, Vector2 Position)> mTanks = new();
    private readonly List<(Entity Table, Vector2 Position)> mStandingTables = new();

    public void Update(World world, GameTime gameTime)
    {
        mWorld = world;
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

        long frameCount = (long)(gameTime.TotalGameTime.TotalMilliseconds / 16.66667);

        // 1. Process dead enemies first to ensure we don't logic-update entities that should be removed.
        world.Query(in sDeadEnemiesQuery,
            (Entity entity, ref StateComponent state, ref PositionComponent position) =>
            {
                if (state.Current == ActorState.Dead)
                {
                    HandleEnemyDeath(entity, position.Value, mWorld, gridMap);
                }
            });

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // 2. Cache global point-of-interests (Tanks & Player) for the current frame.
        mTanks.Clear();
        world.Query(in sTankQuery, (Entity tankEntity, ref PositionComponent tankPos) =>
        {
            mTanks.Add((tankEntity, tankPos.Value));
        });

        mStandingTables.Clear();
        world.Query(in sTableQuery, (Entity tableEntity, ref TableComponent table, ref PositionComponent tablePos,ref InteractableComponent interactable) =>
        {
            if (table.State == TableState.Standing)
            {
                mStandingTables.Add((tableEntity, tablePos.Value));
            }
        });

        mOccupiedCoverSpots.Clear();
        world.Query(in sCoverQuery, (ref CoverBehaviorComponent cover) =>
        {
            if (cover.CurrentCoverPos.HasValue)
            {
                mOccupiedCoverSpots.Add(cover.CurrentCoverPos.Value);
            }
        });

        Vector2 playerPos = Vector2.Zero;
        Entity playerEnt = Entity.Null;
        var playerHealthRatio = 0f;
        world.Query(in sPlayerQueryDesc, (Entity entity, ref PositionComponent pos, ref HealthComponent health) => {
            playerEnt = entity;
            playerPos = pos.Value;
            playerHealthRatio = health.Current / health.Max;
        });

        // 3. Update FlowField navigation if the target has moved significantly (TechDemo optimization).
        if (IsTechDemoMode && flowField != null && playerEnt != Entity.Null)
        {
            float distSq = Vector2.DistanceSquared(playerPos, mLastTargetPosition);
            if (distSq > (UpdateThreshold * UpdateThreshold) || gridMap.Version != mLastGridVersion)
            {
                mFlowFieldService.UpdateFlowField(flowField, playerPos);
                mLastTargetPosition = playerPos;
                mLastGridVersion = gridMap.Version;
            }
        }

        // 4. Cache projectiles to allow enemies to react (e.g., ducking).
        // We filter out projectiles owned by enemies to prevent them from dodging their own bullets.
        Vector2[] projectilePositions = new Vector2[50];
        int projCount = 0;
        world.Query(in sProjectileQuery, (ref PositionComponent pos, ref ProjectileComponent proj) =>
        {
            //muss geprüft werden , sonst crasht es , wenn der Owner tot ist und nicht existiert
            if (!world.IsAlive(proj.Source)){return;}

            if (projCount < 50 && !world.Has<EnemyComponent>(proj.Source) && proj.Nahkampf == false)
            {
                projectilePositions[projCount++] = pos.Value;
            }
        });

        HandleDuckingLogic(world, deltaTime, projectilePositions, projCount);

        // 5. Cache static environment colliders for Line-of-Sight (LoS) checks.
        mStaticObstacles.Clear();
        world.Query(in sStaticColliderQuery,
            (Entity entity,ref PositionComponent pos, ref ColliderComponent col) =>
            {
                if(world.Has<TableComponent>(entity))
                {
                    if (world.Get<TableComponent>(entity).State == TableState.Standing)
                    {
                        return;
                    }
                }
                mStaticObstacles.Add((col.GetAabb(pos.Value), pos.Value));
            });

        // Process each enemy for their path and their aggression.
        world.Query(in sEnemyQueryDesc,
            (Entity entity,
                ref EnemyComponent enemy,
                ref VelocityComponent velocity,
                ref PositionComponent enemyPos,
                ref StateComponent state,
                ref StatusComponent status,
                ref HealthComponent health,
                ref LoadoutComponent loadout,
                ref AmmoComponent ammo,
                ref ColliderComponent collider) =>
            {


                //if hit, knockback without movement and stop cover
                if (state.Current == ActorState.Hit)
                {
                    if (world.Has<CoverBehaviorComponent>(entity))
                    {
                        var cover = world.Get<CoverBehaviorComponent>(entity);
                        cover.CurrentCoverPos = null;
                        cover.CoverCooldown = 2f;
                        cover.IsTakingCover = false;
                        world.Get<CoverBehaviorComponent>(entity) = cover;
                    }
                    return;
                }

                var currentEnemyPos = enemyPos.Value;
                var enemyDef = EnemyDefinitions.Get(enemy.Type);

                var (localSeparation, _) = NavigationHelper.CalculateSeparationForce(
                    spatialHash,
                    entity,
                    true,
                    currentEnemyPos,
                    collider,
                    radius: 35f
                );

                var targetEntity = GetTarget(world, frameCount, entity.Id, currentEnemyPos, ref enemy, ref loadout, ref collider);
                // Fallback: If no target is within the local spatial hash cells, default to global player.
                if (targetEntity == Entity.Null) { targetEntity = playerEnt; }

                // Handle Stunned state (prevents any action or movement).
                if (state.Current == ActorState.Stunned)
                {
                    if (gameTime.TotalGameTime.TotalMilliseconds - state.PersistenceTime > 5000)
                    {
                        state.Current = ActorState.Idle;
                        velocity.Value = velocity.BaseSpeed; state.PersistenceTime = gameTime.TotalGameTime.TotalMilliseconds;
                    }
                    velocity.Value = 0;
                    return;
                }

                // Reset Color and Speed in case Enemy stops being aggravated / frightened
                velocity.Value = velocity.BaseSpeed;

                // Apply ducking velocity change
                if (state.Current == ActorState.Ducking)
                {
                    velocity.Value *= 0.5f;
                }

                // Ensure target is still alive and has required components
                if (!world.IsAlive(targetEntity) ||
                    !world.Has<PositionComponent>(targetEntity) ||
                    !world.Has<HealthComponent>(targetEntity) ||
                    !world.Has<VelocityComponent>(targetEntity))
                {
                    return;
                }

                var targetPositionVector = world.Get<PositionComponent>(targetEntity).Value;
                var targetHealth = world.Get<HealthComponent>(targetEntity);
                var targetVel = world.Get<VelocityComponent>(targetEntity);

                bool isTargetPlayer = (targetEntity == playerEnt);
                bool targetIsMoving = targetVel.Value > 0.1f && targetVel.Direction != Vector2.Zero;
                var targetHealthRatio = targetHealth.Current / targetHealth.Max;

                // Make Enemies faster, if target or player health is low
                if (Math.Min(targetHealthRatio, playerHealthRatio) < enemyDef.EnrageThreshold)
                {
                    velocity.Value = velocity.BaseSpeed * 1.5f;
                }
                // Make Enemies slower, if target and player health is high
                if (Math.Min(targetHealthRatio, playerHealthRatio) > enemyDef.CautionThreshold)
                {
                    velocity.Value = velocity.BaseSpeed * 0.8f;
                }

                // Get aim target and source position that the entities don't shoot from their and to the target's feet.
                var aimTargetPos = world.GetCenter(targetEntity);
                var aimSourcePos = world.GetCenter(entity);

                bool isTakingCover = false;
                if (world.Has<CoverBehaviorComponent>(entity))
                {
                    ref var coverComp = ref world.Get<CoverBehaviorComponent>(entity);
                    isTakingCover = HandleCoverLogic(ref coverComp, ref velocity, ref state, world, entity, currentEnemyPos,
                        targetPositionVector, localSeparation, health, projectilePositions, mOccupiedCoverSpots, projCount, deltaTime,
                        targetHealth.Current, targetHealth.Max, enemyDef.CautionThreshold,
                        gridMap);
                }

                // If taking cover, apply separation to avoid clumping while stationary/hiding.
                //add to occupied spot list in range , um nicht zu blockieren beim covern von mehreren
                if (isTakingCover)
                {
                    foreach (var x in gridMap.AreaWorldtoWorld((Vector2)world.Get<CoverBehaviorComponent>(entity).CurrentCoverPos, 2))
                    {


                        mOccupiedCoverSpots.Add(x);
                    }

                }

                float distToTarget = Vector2.Distance(currentEnemyPos, targetPositionVector);

                // Weapon logic
                var canUseRanged = loadout.HasRanged && (!loadout.Ranged.UsesAmmo || ammo.Current > 0);
                var stopDistance = canUseRanged ? loadout.Ranged.AttackRange : (loadout.HasMelee ? loadout.Melee.AttackRange : 0f);

                // CEO stop distance depends on player movement.
                if (enemy.Type == EnemyType.Ceo)
                {
                    stopDistance = targetIsMoving ? ItemDefinitions.Get(ItemType.AcidSpit).AttackRange : ItemDefinitions.Get(ItemType.LaserArm).AttackRange;
                }

                if (enemy.Type == EnemyType.Ceo  || enemy.Type == EnemyType.Mutant1 || enemy.Type == EnemyType.Mutant2 || enemy.Type == EnemyType.Mutant3)
                {
                    bool interacting = HandleEnviromentInteraction(
                        world,
                        entity,
                        enemy.Type,
                        ref velocity,
                        ref state,
                        currentEnemyPos,
                        targetPositionVector,
                        localSeparation,
                        gridMap);

                        if (interacting) { return; }
                }

                // Check if within range and line-of-sight is clear. Also, don't stop in Acid
                var inRange = distToTarget <= stopDistance * 0.95f;
                var hasSight = CombatExtensions.HasLineOfSight(aimSourcePos, aimTargetPos, mStaticObstacles);
                var isInAcid = status.Types.Any(s => s.Item1 == StatusType.InAcid);

                if (inRange && hasSight && !isInAcid)
                {
                    //um nicht stehen zu bleiben wenn covern
                    if (!isTakingCover)
                    {
                        velocity.Direction = Vector2.Zero; // Stop moving to attack.
                    }

                    if (state.Current != ActorState.Attacking && state.Current != ActorState.Hit) { state.Current = ActorState.Idle; }

                    ItemDefinition weaponToUse = null;
                    var direction = aimTargetPos - aimSourcePos;

                    // Priority: Use melee if close enough, otherwise ranged.
                    if (loadout.HasMelee && distToTarget <= loadout.Melee.AttackRange)
                    {
                        weaponToUse = loadout.Melee;
                    }
                    else if (canUseRanged)
                    {
                        weaponToUse = loadout.Ranged;
                    }

                    // CEO brain
                    if (enemy.Type == EnemyType.Ceo)
                    {
                        if (loadout.HasMelee && distToTarget <= loadout.Melee.AttackRange)
                        {
                            weaponToUse = loadout.Melee;
                        }

                        else if (targetIsMoving)
                        {
                            weaponToUse = ItemDefinitions.Get(ItemType.AcidSpit);
                        }
                        else
                        {
                            weaponToUse = ItemDefinitions.Get(ItemType.LaserArm);
                        }
                    }

                    if (weaponToUse == null) { return; }

                    // UseWeapon erledigt den Rest (Projektilart, AOE, etc.)
                    if (world.UseWeapon(entity, weaponToUse, direction, factoryService, audio))
                    {
                        state.Previous = state.Current;
                        state.Current = ActorState.Attacking;
                    }

                    return;
                }

                //movement ist schon bei handlecover
                if (isTakingCover)
                {
                    return;
                }

                // Use the local separation force gathered from the Spatial Hash to avoid other enemies while moving.
                if (IsTechDemoMode && isTargetPlayer && flowField != null)
                {
                    NavigationHelper.MoveToPlayer(world, entity, currentEnemyPos, targetPositionVector, 0f, gridMap, flowField, true,
                        ref velocity, ref state, localSeparation);
                }
                else
                {
                    NavigationHelper.MoveToTarget(world, entity, currentEnemyPos, targetPositionVector, gridMap,
                        ref velocity, ref state, localSeparation);
                }

                // Ducking fix
                if (state.Current == ActorState.Walking && state.Previous == ActorState.Ducking)
                {
                    if (world.Has<DuckBehaviorComponent>(entity) && world.Get<DuckBehaviorComponent>(entity).ActionTimer > 0)
                    {
                        state.Current = ActorState.Ducking;
                    }
                }
            });
    }

    private void HandleEnemyDeath(Entity enemy, Vector2 position, World world, GridMap gridMap)
    {
        if (world.Has<MovementSoundComponent>(enemy))
        {
            ref var sound = ref world.Get<MovementSoundComponent>(enemy);
            if (sound.WalkSoundInstance != null)
            {
                audio.StopSfxInstance(sound.WalkSoundInstance);
                sound.WalkSoundInstance = null;
            }
        }

        var enemyType = world.Get<EnemyComponent>(enemy).Type;
        var ammo = world.Get<AmmoComponent>(enemy).Current;

        var corpseFactory = new CorpseFactory(content, gridMap);
        corpseFactory.Create(
            world,
            position,
            enemyType,
            ammo,
            rng
        );

        world.Destroy(enemy);
    }

    /// <summary>
    /// Check for ducking requirements and apply ducking values
    /// </summary>
    private void HandleDuckingLogic(World world, float deltaTime, Vector2[] projectilePositions, int projCount)
    {
        world.Query(in sDuckingQuery, (Entity entity,
            ref DuckBehaviorComponent duck,
            ref StateComponent state,
            ref HitBoxComponent hitbox,
            ref SpriteComponent sprite,
            ref PositionComponent pos) =>
        {
            if (duck.CooldownTimer > 0) { duck.CooldownTimer -= deltaTime; }

            if (duck.ActionTimer > 0)
            {
                duck.ActionTimer -= deltaTime;
                // Ending duck
                if (duck.ActionTimer <= 0)
                {
                    hitbox.Size = duck.OriginalHitboxSize;
                    hitbox.Offset = duck.OriginalHitboxOffset;
                    if (state.Current != ActorState.Hit)
                    {
                        state.Current = ActorState.Idle;
                    }
                    duck.CooldownTimer = 3.0f;
                }
                else
                {
                    if (state.Current != ActorState.Hit && state.Current != ActorState.Dead)
                    {
                        state.Current = ActorState.Ducking;
                    }
                }
            }
            // Look for ducking requirements
            else if (duck.CooldownTimer <= 0 && state.Current != ActorState.Stunned && state.Current != ActorState.Hit)
            {
                bool dangerNear = false;
                float reactionSq = duck.ReactionRange * duck.ReactionRange;

                for (int i = 0; i < projCount; i++)
                {
                    if (Vector2.DistanceSquared(pos.Value, projectilePositions[i]) < reactionSq)
                    {
                        dangerNear = true;
                        break;
                    }
                }

                if (dangerNear)
                {
                    if (Random.Shared.NextDouble() < duck.Probability)
                    {
                        // Start Ducking
                        duck.ActionTimer = duck.Duration;
                        duck.OriginalHitboxSize = hitbox.Size;
                        duck.OriginalHitboxOffset = hitbox.Offset;

                        hitbox.Size = new Vector2(hitbox.Size.X * 0.6f, hitbox.Size.Y * 0.6f);

                        state.Current = ActorState.Ducking;
                    }
                    else{
                        duck.CooldownTimer = 0.5f;
                    }
                }
            }
        });
    }

    private bool HandleEnviromentInteraction(
        World world,
        Entity enemy,
        EnemyType type,
        ref VelocityComponent velocity,
        ref StateComponent state,
        Vector2 enemyPos,
        Vector2 targetPos,
        Vector2 separationForce,
        GridMap gridMap)
    {
        Entity bestObject = Entity.Null;
        Vector2 objectPos = Vector2.Zero;
        float distToTarget = Vector2.DistanceSquared(enemyPos, targetPos);
        float bestObjDistSq = distToTarget;

        foreach (var (tankEntity, tankPos) in mTanks)
        {
            if (!world.IsAlive(tankEntity) || !world.Has<InteractableComponent>(tankEntity))
            {
                continue;
            }

            float dSq = Vector2.DistanceSquared(enemyPos, tankPos);
            var interact = world.Get<InteractableComponent>(tankEntity);

            if (dSq < bestObjDistSq && dSq < interact.Radius * interact.Radius)
            {
                bestObjDistSq = dSq;
                bestObject = tankEntity;
                objectPos = tankPos;
            }
        }

        foreach (var (tableEntity, tablePos) in mStandingTables)
        {
            if (!world.IsAlive(tableEntity) || !world.Has<InteractableComponent>(tableEntity))
            {
                continue;
            }

            float dSq = Vector2.DistanceSquared(enemyPos, tablePos);
            var interact = world.Get<InteractableComponent>(tableEntity);

            if (dSq < bestObjDistSq && dSq < interact.Radius * interact.Radius)
            {
                bestObjDistSq = dSq;
                bestObject = tableEntity;
                objectPos = tablePos;
            }
        }

        if (bestObject != Entity.Null)
        {
            if (bestObjDistSq < 1000f)
            {
                if (world.Has<ChemicalTankComponent>(bestObject))
                {
                    ref var tank = ref world.Get<ChemicalTankComponent>(bestObject);
                    tank.State = TankState.Destroyed;

                    if (world.Has<SimpleAnimationComponent>(bestObject))
                    {
                        ref var animation = ref world.Get<SimpleAnimationComponent>(bestObject);
                        animation.CurrentFrame = 0;
                        animation.IsFinished = false;
                    }
                }
                else if (world.Has<TableComponent>(bestObject))
                {
                    ref var table = ref world.Get<TableComponent>(bestObject);
                    table.State = TableState.Flipped;
                }

                world.Remove<InteractableComponent>(bestObject);
                velocity.Direction = Vector2.Zero;
            }
            else
            {
                NavigationHelper.MoveToTarget(world, enemy, enemyPos, objectPos, gridMap,
                    ref velocity, ref state, separationForce);
            }
            return true;
        }
        return false;
    }

    private bool HandleCoverLogic(
        ref CoverBehaviorComponent cover,
        ref VelocityComponent velocity,
        ref StateComponent state,
        World world,
        Entity entity,
        Vector2 currentPos,
        Vector2 playerPos,
        Vector2 separationforce,
        HealthComponent health,
        Vector2[] projectiles,
        List<Vector2> nearbyList,
    int projCount,
        float dt,
        float playerCurrentHp,
        float playerMaxHp,
        float intimidationThreshold,
        GridMap gridMap
        )
    {
        if (cover.CoverCooldown > 0) { cover.CoverCooldown -= dt; }

        // Stay covered until goal is reached or player too close
        if (cover.IsTakingCover)
        {
            if (cover.CurrentCoverPos.HasValue)
            {
                float distToCover = Vector2.Distance(currentPos, cover.CurrentCoverPos.Value);

                // Stop taking cover if player got line of sight
                if (CombatExtensions.HasLineOfSight(playerPos, cover.CurrentCoverPos.Value, mStaticObstacles))
                {
                    cover.IsTakingCover = false;
                    cover.CurrentCoverPos = null;
                    cover.CoverCooldown = 1.0f;
                    return false;
                }

                if (distToCover < 10f)
                {
                    separationforce.Rotate(float.Pi / 10);
                    velocity.Direction = separationforce * 3;
                    state.Current = ActorState.Idle;
                    return true;
                }
                else
                {
                    if (distToCover < 50f && separationforce != Vector2.Zero)
                    {
                        separationforce.Rotate(float.Pi / 10);
                        velocity.Direction = separationforce * 3;
                        state.Current = ActorState.Walking;
                        return true;
                    }
                    velocity.Value = velocity.BaseSpeed * 4f;

                    NavigationHelper.MoveToTarget(world, entity, currentPos, cover.CurrentCoverPos.Value, gridMap,
                        ref velocity, ref state, separationforce / 10, nearbyList);
                    return true;
                }
            }
            else
            {
                cover.IsTakingCover = false;
            }
        }

        // Take new cover?
        if (cover.CoverCooldown <= 0)
        {
            bool isPlayerIntimidating = playerCurrentHp > (playerMaxHp * intimidationThreshold);

            if (!isPlayerIntimidating) { return false; }

            bool underFire = false;
            const float detectionRangeSq = 250f * 250f;

            for (int i = 0; i < projCount; i++)
            {
                if (Vector2.DistanceSquared(currentPos, projectiles[i]) < detectionRangeSq)
                {
                    underFire = true;
                    break;
                }
            }

            // Take cover if requirements are met
            bool lowHealth = (health.Current / health.Max) < 0.5f;

            if (underFire || (lowHealth && Random.Shared.NextDouble() < cover.CoverPreference))
            {
                Vector2? bestSpot = FindBestCoverSpot(currentPos, playerPos, cover.SearchRadius, nearbyList, gridMap);

                if (bestSpot.HasValue)
                {
                    cover.IsTakingCover = true;
                    cover.CurrentCoverPos = bestSpot.Value;
                    return true;
                }
                cover.CoverCooldown = 0.5f;
            }
        }

        return false;
    }

    private Vector2? FindBestCoverSpot(Vector2 enemyPos, Vector2 playerPos, float searchRadius, List<Vector2> occupiedSpots, GridMap gridMap)
    {
        Vector2? bestSpot = null;
        float bestDistScore = float.MaxValue;
        //look in grid for potential spots
        List<Point> potentialSpotList = gridMap.AreaWorldtoGrid(enemyPos,10);
        List<Point> occupiedGridSpotList = [];

        foreach (var x in occupiedSpots)
        {
                occupiedGridSpotList.Add(gridMap.WorldToGrid(x));
        }

        foreach (var obstacle in mStaticObstacles)
        {
            if (Vector2.Distance(enemyPos, obstacle.Position) > searchRadius)
            {
                continue;
            }

            // Vector from player through the obstacle
            Vector2 playerToObstacle = obstacle.Position - playerPos;
            if (playerToObstacle == Vector2.Zero)
            {
                continue;
            }

            Vector2 dir = Vector2.Normalize(playerToObstacle);

            // Calculate position behind obstacle
            float roughRadius = (obstacle.Bounds.Width + obstacle.Bounds.Height) / 4f;
            Vector2 potentialSpot = obstacle.Position + (dir * (roughRadius + 45f));
            potentialSpotList.Add(gridMap.WorldToGrid(potentialSpot));
        }

        //remove occupied and unwalkable spots and those which have a weight
        potentialSpotList.RemoveAll(point =>
        {
            if (occupiedGridSpotList.Contains(point)) { return true; }
            if (!gridMap.IsWalkable(point.X, point.Y)) { return true; }
            return gridMap.GetCellWeight(point.X, point.Y) >= 1;
        });

        foreach (Point potentialSpot  in potentialSpotList)
        {
            var worldPos = gridMap.GridToWorld(potentialSpot);

            var distToSpot = Vector2.DistanceSquared(enemyPos, worldPos);
            if (distToSpot >= bestDistScore) {continue; }

            if (CombatExtensions.HasLineOfSight(playerPos, worldPos, mStaticObstacles)) { continue; }
            if (Pathfinder.FindPath(enemyPos, worldPos, gridMap, occupiedSpots) == null) { continue; }

            bestDistScore = distToSpot;
            bestSpot = worldPos;
        }
        return bestSpot;
    }

    Entity GetTarget(World world, long frameCount, int entityId, Vector2 currentEnemyPos,
        ref EnemyComponent enemy, ref LoadoutComponent loadout, ref ColliderComponent collider)
    {
        var shouldSearchTarget = (entityId + frameCount) % TargetUpdateInterval == 0;

        if (enemy.TargetEntity != Entity.Null && !world.IsAlive(enemy.TargetEntity))
        {
            enemy.TargetEntity = Entity.Null;
        }

        // Don't search if it's not your turn and you already have a target.
        if (!shouldSearchTarget && enemy.TargetEntity != Entity.Null) { return enemy.TargetEntity; }

        var searchRadius = 500f;
        if (loadout.HasRanged)
        {
            searchRadius = Math.Min(loadout.Ranged.AttackRange, MaxVisionRange);
        }

        var targetRect = collider.GetAabb(currentEnemyPos);
        targetRect.Inflate((int)searchRadius, (int)searchRadius);

        mNearbyBuffer.Clear();
        spatialHash.GetEntitiesInRect(targetRect, mNearbyBuffer);

        var minTargetDistSq = float.MaxValue;
        var bestNewTarget = Entity.Null;

        foreach (var entry in mNearbyBuffer)
        {
            if ((entry.mFlags & SpatialFlags.Friend) == 0) { continue; }
            if (entry.mEntity == Entity.Null || !world.IsAlive(entry.mEntity)) { continue; }

            var distSq = Vector2.DistanceSquared(currentEnemyPos, entry.mPosition);
            if (distSq >= minTargetDistSq) { continue; }

            minTargetDistSq = distSq;
            bestNewTarget = entry.mEntity;
        }

        enemy.TargetEntity = bestNewTarget;
        return bestNewTarget;
    }

    public void Dispose()
    {
        // Dispose logic, if needed.
    }

    public void Initialize()
    {
        // Initialization logic, if needed.
    }
}