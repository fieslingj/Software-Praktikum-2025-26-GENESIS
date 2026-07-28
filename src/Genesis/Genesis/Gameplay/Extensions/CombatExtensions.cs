using System;
using System.Collections.Generic;
using System.Diagnostics;
using Arch.Core;
using Genesis.Gameplay.Components;
using Microsoft.Xna.Framework;

namespace Genesis.Gameplay.Extensions;

public static class CombatExtensions
{
    public static void InflictDamage(this World world, Entity target, DamagePayload payload, double currentTime)
    {
        // Safety Check: Ensure target is valid before acting on its components and that Entityy has health
        if (!world.IsAlive(target) || !world.Has<HealthComponent>(target)) { return; }

        // Process bloodlust progression for the attacker (player) if applicable.
        UpdateBloodlustTracker(world, payload, currentTime, target);

        // Queue the damage payload into the target's buffer for processing.
        ApplyDamageToBuffer(world, target, payload);
    }

    /// <summary>
    /// Updates the attacker's bloodlust progression by tracking damage dealt within a sliding time window.
    /// Unlocks the bloodlust state if the damage threshold is reached.
    /// </summary>
    private static void UpdateBloodlustTracker(World world, DamagePayload payload, double currentTime,Entity target)
    {
        if (payload.Source == Entity.Null ||
            !world.IsAlive(payload.Source) ||
            !world.Has<PlayerTagComponent>(payload.Source) ||
            !world.Has<BloodlustTrackerComponent>(payload.Source))
        {
            return;
        }
        //no selfinflict
        if (world.IsAlive(target)){if(payload.Source == target){return;}}
        
        
        //max current hp of target, health is updated after bl
        var currentHp = world.Get<HealthComponent>(target).Current;
        float value =  currentHp < payload.Value ? currentHp : payload.Value;

        ref var tracker = ref world.Get<BloodlustTrackerComponent>(payload.Source);

        // If bloodlust is already unlocked, no need to track further.
        if (tracker.IsUnlocked)
        {
            return;
        }

        // Add hit to the history
        tracker.HitBuffer.Enqueue((currentTime, value));

        // Remove old hits (Sliding Window)
        while (tracker.HitBuffer.Count > 0 &&
               (currentTime - tracker.HitBuffer.Peek().TimeStamp > BloodlustTrackerComponent.WindowDuration))
        {
            var oldHit = tracker.HitBuffer.Dequeue();
        }

        // Check target
        if (!(tracker.CurrentDamageSum(currentTime) >= BloodlustTrackerComponent.DamageTarget)) return;
        
        tracker.IsUnlocked = true;
        Debug.WriteLine(">>> BLOODLUST UNLOCKED! <<<");
    }

    private static void ApplyDamageToBuffer(World world, Entity target, DamagePayload payload)
    {
        if (world.Has<DamageBufferComponent>(target))
        {
            ref var buffer = ref world.Get<DamageBufferComponent>(target);

            // Add hit to buffer, unless the buffer is full already
            if (buffer.HitsCount < DamageBufferComponent.MaxHits)
            {
                buffer.mHits[buffer.HitsCount] = payload;
                buffer.HitsCount++;
            }
            else
            {
#if DEBUG
                Debug.WriteLine(
                    $"[WARNING]: Damage Buffer Overflow! Entity {target.Id} received too many hits in a single frame. Damage discarded."
                );
#endif
            }
        }
        else
        {
            var buffer = new DamageBufferComponent();

            buffer.mHits[0] = payload;
            buffer.HitsCount = 1;

            world.Add(target, buffer);
        }
    }

    /// <summary>
    /// Check, whether there is an obstacle between two positions.
    /// </summary>
    public static bool HasLineOfSight(Vector2 start, Vector2 end, List<(Rectangle Bounds, Vector2 Position)> obstacles)
    {
        foreach (var obstacle in obstacles)
        {
            if (LineIntersectsRect(start, end, obstacle.Bounds)) { return false; }
        }
        return true;
    }
    
    /// <summary>
    /// Returns the logical center of the entity.
    /// If a HitBoxComponent exists, returns Position + Offset.
    /// Otherwise, return just Position (feet).
    /// </summary>
    public static Vector2 GetCenter(this World world, Entity entity, Vector2 pos)
    {
        if (world.Has<HitBoxComponent>(entity))
        {
            return pos + world.Get<HitBoxComponent>(entity).Offset;
        }
        return pos;
    }
    public static Vector2 GetCenter(this World world, Entity entity)
    {
        return GetCenter(world, entity, world.Get<PositionComponent>(entity).Value);
    }

    /// <summary>
    /// Checks whether a line from p1 to p2 collides with a rectangle r.
    /// </summary>
    private static bool LineIntersectsRect(Vector2 p1, Vector2 p2, Rectangle r)
    {
        // If the line is avoiding the rectangle completely,
        // we do not need the expensive intersection calculation.
        float lineMaxX = Math.Max(p1.X, p2.X);
        float lineMinX = Math.Min(p1.X, p2.X);
        float lineMaxY = Math.Max(p1.Y, p2.Y);
        float lineMinY = Math.Min(p1.Y, p2.Y);

        if (lineMaxX < r.Left || lineMinX > r.Right || lineMaxY < r.Top || lineMinY > r.Bottom)
        {
            return false;
        }

        // Detailed check, whether the line crosses one of the sides of the rectangle
        return SegmentIntersect(p1, p2, new Vector2(r.Left, r.Top), new Vector2(r.Right, r.Top)) ||
               SegmentIntersect(p1, p2, new Vector2(r.Right, r.Top), new Vector2(r.Right, r.Bottom)) ||
               SegmentIntersect(p1, p2, new Vector2(r.Right, r.Bottom), new Vector2(r.Left, r.Bottom)) ||
               SegmentIntersect(p1, p2, new Vector2(r.Left, r.Bottom), new Vector2(r.Left, r.Top));
    }

    /// <summary>
    /// Check whether two different line segments intersect.
    /// Line1 from a to b. Line2 from c to d.
    /// For that we use Cramer's rule.
    /// </summary>
    private static bool SegmentIntersect(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        // For Cramer's rule we need the determinant as a denominator.
        float denominator = ((b.X - a.X) * (d.Y - c.Y)) - ((b.Y - a.Y) * (d.X - c.X));

        // If the denominator is zero, the lines are parallel.
        // Two parallel lines never intersect, or the lie on top of each other, we ignore that.
        if (denominator == 0) { return false; }

        // We apply Cramer's rule. We replace each column of the matrix,
        // with the result vector in order to isolate the unknowns r and s.
        float numerator1 = ((a.Y - c.Y) * (d.X - c.X)) - ((a.X - c.X) * (d.Y - c.Y));
        float numerator2 = ((a.Y - c.Y) * (b.X - a.X)) - ((a.X - c.X) * (b.Y - a.Y));

        float r = numerator1 / denominator;
        float s = numerator2 / denominator;

        // If both r AND s lie between 0 and 1,
        // then the intersection point is on both visible line segments.
        return (r >= 0 && r <= 1) && (s >= 0 && s <= 1);
    }
}