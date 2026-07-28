using Genesis.Gameplay.Components;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Arch.Core;

namespace Genesis.Gameplay.Navigation;

public static class NavigationHelper
{
    private static readonly List<SpatialEntry> sSeparationBuffer = new(64);
    
    // Tolerance for considering a waypoint "reached".
    private const float WaypointTolerance = 4f;

    /// <summary>
    /// Move the entity in the direction of the path to the target position.
    /// </summary>
    public static void MoveToTarget(World world, Entity entity, Vector2 currentPos, Vector2 targetPos, GridMap map,
        ref VelocityComponent vel, ref StateComponent state, Vector2 separationForce, List<Vector2> avoidPoints = null)
    {
        if (!world.Has<PathComponent>(entity))
        {
            world.Add(entity, new PathComponent { Waypoints = [], CurrentWaypointIndex = 0 });
        }

        ref var pathComp = ref world.Get<PathComponent>(entity);

        var pathDirection = NavigationHelper.GetDirectionToTarget(
            currentPos,
            targetPos,
            map,
            ref pathComp,avoidPoints
        );

        NavigationHelper.ApplyMovement(
            ref vel,
            ref state,
            pathDirection,
            separationForce
        );
    }

    /// <summary>
    /// Calculates separation force to avoid stacking with other actors.
    /// Uses the SpatialHash for efficient neighbor lookup.
    /// </summary>
    public static (Vector2 Force, int Count) CalculateSeparationForce(
        SpatialHash spatialHash,
        Entity currentEntity,
        bool isEnemy,
        Vector2 currentPos,
        ColliderComponent collider,
        float radius = 20f)
    {
        var radiusSq = radius * radius;
        const float epsilon = 0.001f;

        var separationRect = collider.GetAabb(currentPos);
        separationRect.Inflate((int)radius, (int)radius);

        sSeparationBuffer.Clear();
        spatialHash.GetEntitiesInRect(separationRect, sSeparationBuffer);

        var forceX = 0f;
        var forceY = 0f;

        var count = 0;
        foreach (var entry in sSeparationBuffer)
        {
            // Don't separate from yourself
            if (entry.mEntity == currentEntity) { continue; }
            
            // Only separate with the same team
            // If I am a friend and the other is not a friend, continue
            // If I am an enemy and the other is not an enemy, continue
            if ((!isEnemy && (entry.mFlags & SpatialFlags.Friend) == 0) ||
                (isEnemy && (entry.mFlags & SpatialFlags.Enemy) == 0)) { continue; }

            var dx = currentPos.X - entry.mPosition.X;
            var dy = currentPos.Y - entry.mPosition.Y;
            var distSq = dx * dx + dy * dy;

            if (distSq >= radiusSq || distSq <= epsilon) { continue; }
            count++;
            
            var factor = 1.0f / (distSq + 0.1f);
            forceX += dx * factor;
            forceY += dy * factor;
        }

        var localSeparation = new Vector2(forceX, forceY);

        if (localSeparation == Vector2.Zero) { return (localSeparation, count); }

        localSeparation.Normalize();
        localSeparation.Rotate(float.Pi / 10f);

        return (localSeparation, count);
    }

    // ----- Help functions, to reduce complexity of GetDirectionToTarget -----

    /// <summary>
    /// Calculates the direction to the target based on the current path state.
    /// Handles path recalculation and waypoint traversal.
    /// </summary>
    private static Vector2 GetDirectionToTarget(
        Vector2 currentWorldPos,
        Vector2 targetWorldPos,
        GridMap grid,
        ref PathComponent pathComp, List<Vector2> avoidPoints)
    {
        Point targetGridPos = grid.WorldToGrid(targetWorldPos);

        // Calculate the path, if we don't have a path yet or
        // the player has moved to a different positon since the last calculation.
        // That saves us from many unnecessary expensive A* calculations.
        if (NeedsPathRecalculation(pathComp, targetGridPos, grid) || avoidPoints != null)
        {
            var startGridPos = grid.GetClosestWalkableGridPoint(currentWorldPos);
            var safeStartPos = grid.GridToWorld(startGridPos);
            
            var newPath = Pathfinder.FindPath(safeStartPos, targetWorldPos, grid, avoidPoints);
            UpdatePath(ref pathComp, newPath, targetGridPos, grid.Version);
        }

        // The following logic is for following the waypoints to the target.
        if (TryGetWaypointDirection(currentWorldPos, ref pathComp, out Vector2 direction))
        {
            return direction;
        }

        // Failsafe, for emergency:
        // If for what ever reason we can't calculate a path
        // or the waypoint list is empty.
        // For example if the player appears unreachable because of a bug.
        // Instead of freezing we try a direct line,
        // so that the enemy appears lively.
        //  directDirection = targetWorldPos - currentWorldPos
        return GetNormalizedDirection(targetWorldPos - currentWorldPos);
    }

    /// <summary>
    /// Combines path direction and separation force, applies the result to velocity,
    /// and handles the state transition (Idle <-> Walking).
    /// </summary>
    private static void ApplyMovement(
        ref VelocityComponent velocity,
        ref StateComponent state,
        Vector2 pathDirection,
        Vector2 separationForce)
    {
        Vector2 finalDirection = pathDirection;

        // If there is a separation force, combine it with the path direction.
        // We check against Zero to avoid unnecessary calculations.
        if (separationForce != Vector2.Zero)
        {
            // Factor 1.5f implies separation has a slightly higher priority
            // than the path to avoid stacking.
            finalDirection = pathDirection + (separationForce * 2f);
        }

        if (finalDirection != Vector2.Zero)
        {
            // Normalize to ensure consistent speed (prevents moving too fast diagonally)
            finalDirection.Normalize();
            velocity.Direction = finalDirection;

            // Handle State Transition: Idle -> Walking
            if (state.Current != ActorState.Walking && state.Current != ActorState.Hit)
            {
                state.Previous = state.Current;
                state.Current = ActorState.Walking;
            }
        }
        else
        {
            velocity.Direction = Vector2.Zero;

            // Handle State Transition: Walking -> Idle
            if (state.Current != ActorState.Idle && state.Current != ActorState.Hit)
            {
                state.Previous = state.Current;
                state.Current = ActorState.Idle;
            }
        }
    }

    /// <summary>
    /// Determines if the current path is invalid or the target has moved to a different grid cell.
    /// Also checks if the grid map itself has changed (e.g. doors opened, walls destroyed).
    /// </summary>
    private static bool NeedsPathRecalculation(in PathComponent pathComp, Point targetGridPos, GridMap grid)
    {
        return pathComp.LastTargetGridPosition != targetGridPos ||
               pathComp.LastGridVersion != grid.Version;
    }

    /// <summary>
    /// Resets the path component with new waypoints.
    /// </summary>
    private static void UpdatePath(ref PathComponent pathComp, List<Vector2> newPath, Point targetGridPos, int gridVersion)
    {
        pathComp.Waypoints = newPath;
        pathComp.CurrentWaypointIndex = pathComp.Waypoints is { Count: > 1 } ? 1 : 0;
        pathComp.LastTargetGridPosition = targetGridPos;
        pathComp.LastGridVersion = gridVersion; // Hier wird die Version final gespeichert
    }

    /// <summary>
    /// Attempts to find the next valid direction based on the waypoint list.
    /// </summary>
    private static bool TryGetWaypointDirection(Vector2 currentPos, ref PathComponent pathComp, out Vector2 direction)
    {
        direction = Vector2.Zero;
        if (pathComp.Waypoints == null || pathComp.CurrentWaypointIndex >= pathComp.Waypoints.Count)
        {
            return false;
        }

        Vector2 target = pathComp.Waypoints[pathComp.CurrentWaypointIndex];

        // Progression check: If close enough to the current waypoint, move tot the next one.
        if (Vector2.Distance(currentPos, target) < WaypointTolerance)
        {
            pathComp.CurrentWaypointIndex++;
            // Bounds check for the next waypoint.
            if (pathComp.CurrentWaypointIndex < pathComp.Waypoints.Count)
            {
                target = pathComp.Waypoints[pathComp.CurrentWaypointIndex];
            }
        }

        direction = GetNormalizedDirection(target - currentPos);
        return true;
    }

    /// <summary>
    /// Safely normlizes a vector.
    /// </summary>
    private static Vector2 GetNormalizedDirection(Vector2 direction)
    {
        if (direction != Vector2.Zero)
        {
            direction.Normalize();
        }
        return direction;
    }

    /// <summary>
    /// Handles the movement towards the player for the companions.
    /// If the companion is close enough it should stop.
    /// If not, it should move towards the player.
    /// Differantiates between pathfinde (A*) and a Goal-based VectorField (FlowField).
    /// In the Techdemo the FlowField should be used.
    /// </summary>
    public static void MoveToPlayer(
        World world,
        Entity entity,
        Vector2 currentPos,
        Vector2 playerPos,
        float stopDistance,
        GridMap gridMap,
        FlowField flowField,
        bool isTechDemo,
        ref VelocityComponent vel,
        ref StateComponent state,
        Vector2 separationForce)
    {
        float distToPlayer = Vector2.Distance(currentPos, playerPos);

        if (distToPlayer <= stopDistance)
        {
            vel.Direction = Vector2.Zero;
            if (separationForce != Vector2.Zero)
            {
                vel.Direction += separationForce;
            }

            if (state.Current != ActorState.Hit)
            {
                state.Current = ActorState.Idle;
            }

            // Clear path
            if (world.Has<PathComponent>(entity))
            {
                world.Get<PathComponent>(entity).Waypoints?.Clear();
            }

            return;
        }

        bool usedFlowField = false;

        if (isTechDemo && flowField != null)
        {
            var safeGridPos = gridMap.GetClosestWalkableGridPoint(currentPos);
            if (gridMap.IsValid(safeGridPos.X, safeGridPos.Y))
            {
                var flowDir = flowField.VectorField[safeGridPos.X, safeGridPos.Y];
                if (flowDir != Vector2.Zero)
                {
                    // Apply the movement determined by the FlowField.
                    ApplyMovement(ref vel, ref state, flowDir, separationForce);
                    usedFlowField = true;
                }
            }
        }

        // For the movement when ingame and not in TechDemo. Also a Fallback.
        if (!usedFlowField)
        {
            MoveToTarget(world, entity, currentPos, playerPos, gridMap, ref vel, ref state, separationForce);
        }
    }
}