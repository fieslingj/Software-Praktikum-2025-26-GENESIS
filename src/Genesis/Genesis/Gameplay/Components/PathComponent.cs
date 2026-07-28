using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Genesis.Gameplay.Components;

/// <summary>
/// Saves the current path for an entity.
/// Will be filled by the A* algorithm and be run by the EnemyControlSystem.
/// </summary>
public struct PathComponent
{
    // List of waypoints the entity has to visit to reach the target.
    // Calculated by A*.
    public List<Vector2> Waypoints { get; set; }
    
    // The waypoint the entity is currently moving towards.
    public int CurrentWaypointIndex { get; set; }
    
    /// <summary>
    /// Stores the grid position of the target used during the last path calculation.
    /// We check this against the current player position.
    /// If the player hasn't moved since the last calculation
    /// we don't need to recalculate the path.
    /// Saves expensive A* calculations for an optimized performance.
    /// </summary>
    public Point LastTargetGridPosition { get; set; }
    
    // The version of the grid that the path uses. If grid was updated (higher version number), path should be updated
    public int LastGridVersion { get; set; }
}