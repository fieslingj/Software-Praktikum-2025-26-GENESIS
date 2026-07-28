using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Genesis.Gameplay.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using MonoGame.Extended.Tiled;

namespace Genesis.Gameplay.Navigation;

/// <summary>
/// Represents the navigation grid of the game world.
/// Converts the TiledMaps's collision layer into a 2D boolean array.
/// </summary>
public class GridMap
{
    // The 2D array, represents the walkable area.
    // If mIsWalkable=false, then there is some kind of collision object,
    // which should be avoided.
    private readonly bool[,] mIsWalkable;
    // The lower the weight of a cell, the more preferable the cell is for movement.
    private readonly int[,] mCellWeights;
    
    // The dimensions of the grid
    public int Width { get; }
    public int Height { get; }
    public int CellSize { get; }
    public int Version { get; private set; } = 1;

    /// <param name="precisionDivider">Defines how much more precise the grid is compared to the tile width</param>
    public GridMap(TiledMap map, int precisionDivider=2)
    {
        if (precisionDivider < 1) { precisionDivider = 1; }
        
        // Initialize dimensions based on the tiled map data.
        CellSize = map.TileWidth / precisionDivider;

        var totalMapWidthPixels = map.Width * map.TileWidth;
        var totalMapHeightPixels = map.Height * map.TileHeight;

        Width = totalMapWidthPixels / CellSize;
        Height = totalMapHeightPixels / CellSize;
        
        mIsWalkable = new bool[Width, Height];
        mCellWeights = new int[Width, Height];

        // Initialize all grid cells as walkable and the weights as 0.
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                mIsWalkable[x, y] = true;
                mCellWeights[x, y] = 0;
            }
        }
    }

    /// <summary>
    /// Increases movement weight for all grid cells within a specified rectangular area.
    /// Useful for dynamic obstacles or temporary danger zones that the AI should avoid.
    /// </summary>
    public void AddDynamicWeight(Vector2 centerPos, Vector2 size, int weight)
    {
        Version++;
        
        float left = centerPos.X - (size.X / 2f);
        float top = centerPos.Y - (size.Y / 2f);
        float right = centerPos.X + (size.X / 2f);
        float bottom = centerPos.Y + (size.Y / 2f);

        int startX = Math.Max(0, (int)(left / CellSize));
        int startY = Math.Max(0, (int)(top / CellSize));
        int endX = Math.Min(Width - 1, (int)((right - 0.01f) / CellSize));
        int endY = Math.Min(Height - 1, (int)((bottom - 0.01f) / CellSize));

        // Add smaller weights to the cells next to the main area
        var marginStartX = Math.Max(0, startX - 1);
        var marginStartY = Math.Max(0, startY - 1);
        var marginEndX = Math.Min(Width - 1, endX + 1);
        var marginEndY = Math.Min(Height - 1, endY + 1);
        var marginWeight = Math.Min(1, weight / 2);

        for (var x = marginStartX; x <= marginEndX; x++)
        {
            for (var y = marginStartY; y <= marginEndY; y++)
            {
                var isInner = (x >= startX && x <= endX && y >= startY && y <= endY);
                var addWeight = isInner ? weight : marginWeight;
            
                mCellWeights[x, y] += addWeight;
            }
        }
    }

    public void RemoveDynamicWeight(Vector2 centerPos, Vector2 size, int weight)
    {
        Version++;
        
        float left = centerPos.X - (size.X / 2f);
        float top = centerPos.Y - (size.Y / 2f);
        float right = centerPos.X + (size.X / 2f);
        float bottom = centerPos.Y + (size.Y / 2f);

        int startX = Math.Max(0, (int)(left / CellSize));
        int startY = Math.Max(0, (int)(top / CellSize));
        int endX = Math.Min(Width - 1, (int)((right - 0.01f) / CellSize));
        int endY = Math.Min(Height - 1, (int)((bottom - 0.01f) / CellSize));

        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                mCellWeights[x, y] = Math.Max(0, mCellWeights[x, y] - weight);
            }
        }
    }
    

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetCellWeight(int x, int y)
    {
        return (uint)x < (uint)Width && (uint)y < (uint)Height ? mCellWeights[x, y] : 0;
    }

    /// <summary>
    /// Get the closest walkable grid point. Use this, if entity is stuck in an unwalkable grid point.
    /// </summary>
    public Point GetClosestWalkableGridPoint(Vector2 worldPos)
    {
        var gridPos = WorldToGrid(worldPos);
        if (IsWalkable(gridPos.X, gridPos.Y)) { return gridPos; }
    
        var bestPoint = gridPos;
        var bestDistanceSq = float.MaxValue;
        var found = false;

        for (var x = -1; x <= 1; x++)
        {
            for (var y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) { continue; }

                var nx = gridPos.X + x;
                var ny = gridPos.Y + y;

                if (!IsWalkable(nx, ny)) { continue; }

                var cellCenter = GridToWorld(new Point(nx, ny));
                var distSq = Vector2.DistanceSquared(worldPos, cellCenter);

                if (distSq >= bestDistanceSq) { continue; }

                bestDistanceSq = distSq;
                bestPoint = new Point(nx, ny);
                found = true;
            }
        }
        return found ? bestPoint : gridPos;
    }

    /// <summary>
    /// Mark the GridCell at a given position for a given collider as unwalkable.
    /// Important, since the Props are added after the GridMap is initialized.
    /// Add a weight to the cells next to the collider
    /// </summary>
    public void MarkColliderAsUnwalkable(Vector2 worldPos, in ColliderComponent collider)
    { 
        SetWalkability(worldPos, collider, false);
    }

    /// <summary>
    /// Update the walkability in the GridMap for a specific position.
    /// Can either be set to walkable or unwalkable.
    /// Add a weight to the cells next to the collider
    /// For example: After a door has opened, and before the collider is deleted, set the cell to walkable.
    /// </summary>
    public void SetWalkability(Vector2 worldPos, in ColliderComponent collider, bool isWalkable)
    {
        Version++;
        var bounds = collider.GetAabb(worldPos);
        const int margin = 1;
        const int marginWeight = 100;

        int startX = Math.Max(0, (int)(bounds.Left / CellSize));
        int startY = Math.Max(0, (int)(bounds.Top / CellSize));
        int endX = Math.Min(Width - 1, (int)((bounds.Right - 0.01f) / CellSize));
        int endY = Math.Min(Height - 1, (int)((bounds.Bottom - 0.01f) / CellSize));

        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                mIsWalkable[x, y] = isWalkable;
            }
        }

        int marginStartX = Math.Max(0, startX - margin);
        int marginStartY = Math.Max(0, startY - margin);
        int marginEndX = Math.Min(Width - 1, endX + margin);
        int marginEndY = Math.Min(Height - 1, endY + margin);

        for (int x = marginStartX; x <= marginEndX; x++)
        {
            for (int y = marginStartY; y <= marginEndY; y++)
            {
                // Skip the direct collider cells
                if (x >= startX && x <= endX && y >= startY && y <= endY) { continue; }

                if (!isWalkable)
                {
                    mCellWeights[x, y] = Math.Max(mCellWeights[x, y], marginWeight);
                }
                else
                {
                    if (mCellWeights[x, y] >= marginWeight)
                    {
                        mCellWeights[x, y] = 0;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Checks if a specific grid coordinate is free of collision objects.
    /// Used by A* (Pathfinding) to determine valid neighbors.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsWalkable(int x, int y)
    {
        return IsValid(x, y) && mIsWalkable[x, y];
    }

    /// <summary>
    /// Check to ensure the coordinate exists within the grid array.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValid(int x, int y)
    {
        return (uint)x < (uint)Width && (uint)y < (uint)Height;
    }

    /// <summary>
    /// Converts a world position to a grid coordinate (indices).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Point WorldToGrid(Vector2 worldPos)
    {
        return new Point((int)(worldPos.X / CellSize), (int)(worldPos.Y / CellSize));
    }

    /// <summary>
    /// Converts a grid coordinate back to a world position.
    /// <para>
    /// <b>Note:</b> Adds +TileSize /2f to center the point in the middle of the tile,
    /// to ensure entities move towards the center of a tile.
    /// </para>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 GridToWorld(Point gridPos)
    {
        return new Vector2(
            gridPos.X * CellSize + CellSize / 2f,
            gridPos.Y * CellSize + CellSize / 2f
        );
    }

    //get area of point in grid nearby
    public List<Vector2> AreaGridtoWorld(Point gridPos, int range)
    {
        List<Vector2> list = [];
        for (int x = -range; x < range; x++)
        {
            for (int y = -range; y < range; y++)
            {
                if (IsValid(x+gridPos.X, y+gridPos.Y))
                {
                    if (mIsWalkable[x +gridPos.X, y +gridPos.Y])
                    {
                        list.Add(GridToWorld(new Point(x + gridPos.X, y + gridPos.Y)));
                    }
                }
            }
        }
        return list;
    }
    public List<Point> AreaGridtoGrid(Point gridPos, int range)
    {
        List<Point> list = [];
        for (int x = -range; x < range; x++)
        {
            for (int y = -range; y < range; y++)
            {
                if (IsValid(x+gridPos.X, y+gridPos.Y))
                {
                    if (mIsWalkable[x +gridPos.X, y +gridPos.Y])
                    {
                        list.Add(new Point(x + gridPos.X, y + gridPos.Y));
                    }
                }
            }
        }
        return list;
    }

    public List<Vector2> AreaWorldtoWorld(Vector2 worldPos, int range)
    {
        return AreaGridtoWorld(WorldToGrid(worldPos), range);
    }
    public List<Point> AreaWorldtoGrid(Vector2 worldPos, int range)
    {
        return AreaGridtoGrid(WorldToGrid(worldPos), range);
    }
}