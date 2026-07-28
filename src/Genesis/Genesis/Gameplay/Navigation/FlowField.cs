using System;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace Genesis.Gameplay.Navigation;

/// <summary>
/// Data-structure to save both the cost from each cell to the goal,
/// aswell as the direction vector for each cell.
/// </summary>
public class FlowField
{
    public int Width { get; }
    public int Height { get; }
    // Costs for each cell towards the goal.
    // int.MaxValue represents unreachable or non-walkable areas.
    public int[ , ] IntegrationField { get; }
    // Direction vectors for each cell towards the goal.
    public Vector2[ , ] VectorField { get; }
    public GridMap Grid { get;}

    public FlowField(GridMap grid)
    {
        Grid = grid;
        Width = grid.Width;
        Height = grid.Height;
        IntegrationField = new int[Width, Height];
        VectorField = new Vector2[Width, Height];
    }

    /// <summary>
    /// Reset the FlowField to its default values.
    /// Essential before re-calculating for a new goal-position.
    /// </summary>
    public void Reset()
    {
        Array.Clear(VectorField, 0, VectorField.Length);

        Parallel.For(0, Width, x =>
        {
            for (int y = 0; y < Height; y++)
            {
                IntegrationField[x, y] = int.MaxValue;
            }
        });
    }
}