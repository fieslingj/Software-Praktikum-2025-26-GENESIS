using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Genesis.Gameplay.Navigation;

/// <summary>
/// Implements the calculation logic of the goal-based vector fiels
/// by generating a cost field using Dijkstra algorithm and
/// converting it into efficient movement vectors.
/// </summary>
public class FlowFieldService
{
    private readonly Queue<Point> mFrontier = new();
    private static readonly Point[] sNeighborOffsets =
    [
        new(0, -1), new(1, 0), new(0, 1), new(-1, 0), // Cardinal
        new(1, 1), new(-1, 1), new(1, -1), new(-1, -1) // Diagonal
    ];
    private static readonly int[] sNeighborCosts =
    [
        10, 10, 10, 10, // Cardinal
        14, 14, 14, 14  // Diagonal
    ];
    private static readonly Vector2[] sNeighborVectors =
    [
        new(0, -1), new(1, 0), new(0, 1), new(-1, 0),
        Vector2.Normalize(new Vector2(1, 1)), Vector2.Normalize(new Vector2(-1, 1)), 
        Vector2.Normalize(new Vector2(1, -1)), Vector2.Normalize(new Vector2(-1, -1))
    ];

    public void UpdateFlowField(FlowField flowField, Vector2 goalWorldPos)
    {
        // Initialize the field and validate the target position.
        if (!InitializeGoal(flowField, goalWorldPos, out Point goal)){ return; }
        // Dijkstra integration: Determine the costs for each cell from the target moving outwards.
        CaclulateIntegrationField(flowField, goal);
        // Determine the movement vectors based on the costs of the neighbouring cell.
        CalculateVectorFieldParallel(flowField);
    }

    /// <summary>
    /// Resets the FlowField and validates the target position in the grid.
    /// </summary>
    private bool InitializeGoal(FlowField flowField, Vector2 goalWorldPos, out Point goal)
    {
        var grid = flowField.Grid;
        flowField.Reset(); 
        
        goal = grid.WorldToGrid(goalWorldPos);

        if (!grid.IsValid(goal.X, goal.Y)) { return false; }

        mFrontier.Clear();
        mFrontier.Enqueue(goal);
        flowField.IntegrationField[goal.X, goal.Y] = 0;

        return true;
    }

    /// <summary>
    /// Calculates the integration field using the dijkstra algorithm starting from the target point.
    /// </summary>
    private void CaclulateIntegrationField(FlowField flowField, Point goal)
    {
        var grid = flowField.Grid;

        var integrationField = flowField.IntegrationField; 

        while (mFrontier.Count > 0)
        {
            var current = mFrontier.Dequeue();
            var currentX = current.X;
            var currentY = current.Y;
            var currentCost = integrationField[currentX, currentY];

            for (var i = 0; i < 8; i++)
            {
                var nextX = currentX + sNeighborOffsets[i].X;
                var nextY = currentY + sNeighborOffsets[i].Y;

                if (!grid.IsValid(nextX, nextY)) { continue; }
                if (!grid.IsWalkable(nextX, nextY)) { continue; }
                
                var oldCost = integrationField[nextX, nextY];
                var moveCost = sNeighborCosts[i];
                var cellWeight = grid.GetCellWeight(nextX, nextY) * 10;

                var newCost = currentCost + moveCost + (cellWeight * 10);

                if (newCost >= oldCost) { continue; }

                integrationField[nextX, nextY] = newCost;
                mFrontier.Enqueue(new Point(nextX, nextY));
            }
        }
    }

    /// <summary>
    /// Generates the final vector field by searching for the neighbor with the lowest cost for each cell.
    /// </summary>
    private static void CalculateVectorFieldParallel(FlowField flowField)
    {
        var width = flowField.Width;
        var height = flowField.Height;
        var integrationField = flowField.IntegrationField;
        var vectorField = flowField.VectorField;
        var grid = flowField.Grid;

        Parallel.For(0, width, x =>
        {
            for (var y = 0; y < height; y++)
            {
                // Ignore nreachable nodes
                if (integrationField[x, y] == int.MaxValue) 
                {
                    vectorField[x, y] = Vector2.Zero; 
                    continue; 
                }

                // Search locally for cheapest neighbor
                var bestCost = integrationField[x, y];
                var bestDir = Vector2.Zero;

                for (var i = 0; i < 8; i++)
                {
                    var offsetX = sNeighborOffsets[i].X;
                    var offsetY = sNeighborOffsets[i].Y;
                    
                    var nx = x + offsetX;
                    var ny = y + offsetY;

                    if (!grid.IsValid(nx, ny)) { continue; }
                    
                    // Corner cut prevention
                    if (offsetX != 0 && offsetY != 0)
                    {
                        var horizontalWalkable = grid.IsWalkable(x + offsetX, y);
                        var verticalWalkable = grid.IsWalkable(x, y + offsetY);

                        if (!horizontalWalkable || !verticalWalkable)
                        {
                            continue;
                        }
                    }

                    var neighborCost = integrationField[nx, ny];
                    if (neighborCost >= bestCost) { continue; }

                    bestCost = neighborCost;
                    bestDir = sNeighborVectors[i];
                }
                
                vectorField[x, y] = bestDir;
            }
        });
    }
}