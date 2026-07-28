using System;
using Microsoft.Xna.Framework;

namespace Genesis.Architecture;

public class RandomService
{
    private Random mRandom;
    private int mCurrentSeed;
    
    public int Seed => mCurrentSeed;

    public RandomService(int seed)
    {
        SetSeed(seed);
    }

    public void SetSeed(int seed)
    {
        mCurrentSeed = seed;
        mRandom = new Random(seed);
    }
    
    public int Next() => mRandom.Next();
    public int Next(int max) => mRandom.Next(max);
    public int Next(int min, int max) => mRandom.Next(min, max);
    public float NextFloat() => mRandom.NextSingle();
    public double NextDouble() => mRandom.NextDouble();
    public int NextSign() => Next(0, 2) == 0 ? -1 : 1;
    
    public bool Chance(float probability) => mRandom.NextDouble() < probability;
    public float Range(float min, float max) => min + NextFloat() * (max - min);

    /// <summary>
    /// Get a random point inside a circle.
    /// </summary>
    public Vector2 InsideUnitCircle()
    {
        var theta = NextDouble() * 2 * Math.PI;
        var r = Math.Sqrt(NextDouble());
        return new Vector2((float)(r * Math.Cos(theta)), (float)(r * Math.Sin(theta)));
    }

    /// <summary>
    /// Get a random direction vector (normalized)
    /// </summary>
    public Vector2 NextDirection()
    {
        var theta = NextDouble() * 2 * Math.PI;
        return new Vector2((float)Math.Cos(theta), (float)Math.Sin(theta));
    }

    /// <summary>
    /// Get a random position within a rectangle.
    /// </summary>
    public Vector2 InRect(Rectangle rect)
    {
        return new Vector2(
            Range(rect.X, rect.X + rect.Width),
            Range(rect.Y, rect.Y + rect.Height)
        );
    }
}