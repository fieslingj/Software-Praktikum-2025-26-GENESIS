using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Arch.Core;

namespace Genesis.Gameplay.Navigation;

/// <summary>
/// A spatial partitioning structure that organizes entities into a grid.
/// </summary>
public class SpatialHash
{
    private readonly float mInverseCellSize;

    // Dictionary that stores all entities in their according cell, determined by the hash.
    private readonly Dictionary<long, List<SpatialEntry>> mGrid = new();

    /// <summary>
    /// A collection of previously used lists that are currently inactive.
    /// Use a Stack and reuse memory instead of allocating/deallocating it, for performance.
    /// </summary>
    private readonly Stack<List<SpatialEntry>> mListPool = new();

    /// <param name="cellSize"> The size of one grid square in pixels. </param>
    public SpatialHash(int cellSize)
    {
        mInverseCellSize = 1f / cellSize;
    }

    /// <summary>
    /// Combines two 32-bit integers into a single 64-bit long for efficient dictionary lookups.
    /// Pack X into the upper 32 bits and Y into the lower 32 bits.
    /// </summary>
    private long GetKey(Vector2 position)
    {
        int x = (int)Math.Floor(position.X * mInverseCellSize);
        int y = (int)Math.Floor(position.Y * mInverseCellSize);
        return GetKey(x, y);
    }
    
    private long GetKey(int x, int y)
    {
        return ((long)x << 32) | (y & 0xFFFFFFFFL);
    }

    /// <summary>
    /// Clears the grid and returns lists to the pool for reuse.
    /// Must be called at the start of every frame before re-inserting entities.
    /// </summary>
    public void Clear()
    {
        foreach (var list in mGrid.Values)
        {
            list.Clear();
            mListPool.Push(list);
        }
        mGrid.Clear();
    }
    
    /// <summary>
    /// Places an entity into the grid based on its rectangle bounds.
    /// </summary>
    public void Insert(Entity entity, Rectangle aabb, Vector2 position, SpatialFlags flags)
    {
        int startX = (int)Math.Floor(aabb.Left * mInverseCellSize);
        int endX   = (int)Math.Floor((aabb.Right - 1) * mInverseCellSize);
        int startY = (int)Math.Floor(aabb.Top * mInverseCellSize);
        int endY   = (int)Math.Floor((aabb.Bottom - 1) * mInverseCellSize);

        var entry = new SpatialEntry(entity, aabb, position, flags);

        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                AddEntryToCell(GetKey(x, y), entry);
            }
        }
    }
    
    private void AddEntryToCell(long key, SpatialEntry entry)
    {
        if (!mGrid.TryGetValue(key, out var list))
        {
            list = mListPool.Count > 0 ? mListPool.Pop() : [];
            mGrid[key] = list;
        }
        list.Add(entry);
    }

    /// <summary>
    /// Retrieves all entities in the cell of the given position plus all 8 surrounding neighbor cells.
    /// </summary>
    /// <param name="position">Center position to check from.</param>
    /// <param name="result">An existing list to be filled with nearby entities (prevents allocation).</param>
    public void GetNearbyEntities(Vector2 position, List<SpatialEntry> result)
    {
        int cx = (int)Math.Floor(position.X * mInverseCellSize);
        int cy = (int)Math.Floor(position.Y * mInverseCellSize);

        for (int x = cx - 1; x <= cx + 1; x++)
        {
            for (int y = cy - 1; y <= cy + 1; y++)
            {
                long key = GetKey(x, y);
                if (mGrid.TryGetValue(key, out var list))
                {
                    result.AddRange(list);
                }
            }
        }
    }
    
    /// <summary>
    /// Retrieves all entities located in any cell that the given rectangle touches.
    /// Used for precise wall checking and large area queries.
    /// </summary>
    public void GetEntitiesInRect(Rectangle rect, List<SpatialEntry> result)
    {
        int startX = (int)Math.Floor(rect.Left * mInverseCellSize);
        int endX   = (int)Math.Floor((rect.Right - 1) * mInverseCellSize);
        int startY = (int)Math.Floor(rect.Top * mInverseCellSize);
        int endY   = (int)Math.Floor((rect.Bottom - 1) * mInverseCellSize);

        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                long key = GetKey(x, y);
                if (mGrid.TryGetValue(key, out var list))
                {
                    result.AddRange(list);
                }
            }
        }
    }
}

[Flags]
public enum SpatialFlags : byte
{
    None     = 0,
    Static   = 1 << 0,
    Dynamic  = 1 << 1,
    Friend   = 1 << 2,
    Enemy    = 1 << 3,
    Hitbox   = 1 << 4
}