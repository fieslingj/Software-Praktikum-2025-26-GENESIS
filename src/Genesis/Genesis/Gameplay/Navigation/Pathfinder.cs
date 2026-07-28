using System;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Genesis.Gameplay.Navigation;

/// <summary>
/// Class implementing the A* (pathfinder) algorithm.
/// General information about A*.
/// A* uses the following three components:
/// Actual cost g: describes the cost from the current path,
/// from the start point to the current node that we are visiting.
/// Heuristic h: How far is it from the current node to the target.
/// This is an estimate, which always has to be cheaper or the same,
/// as the actual cost to get from the current node to the target.
/// We use the manhattan distance as our heuristic.
/// We can use this, since we don't allow diagonal movement.
/// With diagonal movement, we would have to use the straight line distance,
/// which is quite expensive to calculate.
/// F-Cost: F=g+h.
/// The Loop is as follows.
/// For our current node (starting with the start node as current),
/// we consider all neighbors (Up, Right, Down, Left are sufficient,
/// since diagonal movements are not permitted).
/// We calculate the F value for all neighbors and add all of them to our queue.
/// We always visit the node with the lowest F value,
/// not just the lowest value of the four current neighbors, but all nodes.
/// Then we check if the node is the target or else we repeat the loop.
/// Each node remebers where we came from, so that we can easily backtrack our path.
/// </summary>
public static class Pathfinder
{
    private static readonly PriorityQueue<Point, float> sFrontier = new(4096);
    private static readonly Dictionary<Point, Point> sCameFrom = new(4096);
    private static readonly Dictionary<Point, float> sCostSoFar = new(4096);
    private static readonly HashSet<Point> sAvoidSet = new(512);
    
    private static readonly List<Vector2> sRawPathBuffer = new(2048);
    private static readonly List<Vector2> sSmoothPathBuffer = new(2048);
    
    // Defines four legal directions (Up, Right, Down, Left).
    // Diagonal movements are omitted to simplify the calculations and collision checks,
    // and thus enhance performance.
    private static readonly Point[] sDirections =
    [
        new(0, -1), new(1, 0), new(0, 1), new(-1, 0), // Cardinal
        new(1, 1), new(-1, 1), new(1, -1), new(-1, -1) // Diagonal
    ];

    /// <summary>
    /// Calculate a path from start to ende, using A*.
    /// </summary>
    /// <returns>A list of waypoints, or if no path is found null.</returns>
    public static List<Vector2> FindPath(Vector2 startWorld, Vector2 endWorld, GridMap grid, List<Vector2> avoidPoints)
    {
        // First convert the world positions into grid coordinates.
        Point startNode = grid.WorldToGrid(startWorld);
        Point endNode = grid.WorldToGrid(endWorld);

        // Check if the target is inside a wall or if we are already there.
        if (!grid.IsWalkable(endNode.X, endNode.Y) || startNode == endNode)
        {
            return null;
        }

        sAvoidSet.Clear();
        sFrontier.Clear();
        sCameFrom.Clear();
        sCostSoFar.Clear();
        
        // Get avoid points, so that WorldToGrid is only called once.
        if (avoidPoints != null)
        {
            foreach (var pos in avoidPoints)
            {
                sAvoidSet.Add(grid.WorldToGrid(pos));
            }
        }
        
        sFrontier.Enqueue(startNode, 0);
        sCameFrom[startNode] = startNode;
        sCostSoFar[startNode] = 0;

        while (sFrontier.Count > 0)
        {
            Point current = sFrontier.Dequeue();

            // Stop searching if goal is reached.
            if (current == endNode)
            {
                ReconstructPath(sCameFrom, startNode, endNode, grid);
                return SmoothPath(sRawPathBuffer, grid);
            }

            // Explore the neighbors of the current node.
            foreach (var dir in sDirections)
            {
                Point next = new Point(current.X + dir.X, current.Y + dir.Y);

                if (grid.IsWalkable(next.X, next.Y))
                {
                    if (dir.X != 0 && dir.Y != 0)
                    {
                        if (!grid.IsWalkable(current.X + dir.X, current.Y) || 
                            !grid.IsWalkable(current.X, current.Y + dir.Y))
                        {
                            continue;
                        }
                    }
                    
                    float extracost = sAvoidSet.Contains(next) ? 100 : 0;
                    var moveCost = (dir.X != 0 && dir.Y != 0) ? 1.414f : 1f;
                    var weight = grid.GetCellWeight(next.X, next.Y) + extracost;
                    var newCost = sCostSoFar[current] + moveCost + weight;

                    // If we found a new node,
                    // or a cheaper path to the one we are currently exploring.
                    if (!sCostSoFar.ContainsKey(next) || newCost < sCostSoFar[next])
                    {
                        sCostSoFar[next] = newCost;

                        // Heuristic h. Manhattan distance.
                        // We can use this, because we only allow 4-directional movement.
                        // This satisfies the requirements that h is admissable,
                        // or in other words that h is an optimistic estimate
                        // of the cost that actually occur.
                        float priority = newCost + Math.Abs(endNode.X - next.X)
                                                 + Math.Abs(endNode.Y - next.Y);

                        sFrontier.Enqueue(next, priority);
                        sCameFrom[next] = current;
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Backtrack from the end node to the start node,
    /// using the cameFrom dictionary to construct the list of waypoints.
    /// </summary>
    private static void ReconstructPath(
        Dictionary<Point, Point> cameFrom,
        Point start,
        Point end,
        GridMap grid)
    {
        sRawPathBuffer.Clear();
        Point current = end;

        // Trace backwards as long as the current node is not the start node.
        while (current != start)
        {
            // Convert the grid coordinates back to world positions.
            sRawPathBuffer.Add(grid.GridToWorld(current));
            current = cameFrom[current];
        }

        sRawPathBuffer.Add(grid.GridToWorld(start));
        
        // The list is backwards (from the end to start), reverse and return it.
        sRawPathBuffer.Reverse();
    }
    
    private static List<Vector2> SmoothPath(List<Vector2> originalPath, GridMap grid)
    {
        if (originalPath == null || originalPath.Count < 3) { return originalPath; }

        sSmoothPathBuffer.Clear();
        sSmoothPathBuffer.Add(originalPath[0]);

        var currentIdx = 0;
        while (currentIdx < originalPath.Count - 1)
        {
            bool foundNext = false;
            for (var i = originalPath.Count - 1; i > currentIdx; i--)
            {
                var start = sSmoothPathBuffer[^1];
                var end = originalPath[i];

                // If line of sight to point i
                if (HasLineOfSight(start, end, grid))
                {
                    sSmoothPathBuffer.Add(end);
                    currentIdx = i;
                    foundNext = true;
                    break;
                }

                // Fallback: Take the direct neighbor
                if (i == currentIdx + 1)
                {
                    sSmoothPathBuffer.Add(end);
                    currentIdx = i;
                    foundNext = true;
                }
            }
            if (!foundNext) break;
        }

        return [..sSmoothPathBuffer];
    }
    
    private static bool HasLineOfSight(Vector2 start, Vector2 end, GridMap grid)
    {
        var dist = Vector2.Distance(start, end);
        if (dist < 1f) { return true; }

        var dir = (end - start);
        dir.Normalize();

        var stepSize = grid.CellSize / 2f; 
        for (var d = stepSize; d < dist; d += stepSize)
        {
            var checkPos = start + dir * d;
            var gridPos = grid.WorldToGrid(checkPos);
            if (!grid.IsWalkable(gridPos.X, gridPos.Y) || 
                grid.GetCellWeight(gridPos.X, gridPos.Y) >= 1) { return false; }
        }

        return true;
    }
}