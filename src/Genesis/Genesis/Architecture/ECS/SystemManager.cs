using System;
using System.Collections.Generic;
using System.Linq;
using Arch.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Genesis.Architecture.ECS;

/// <summary>
/// A simple service locator that holds a single, shared instance
/// of every registered <see cref="ISystem"/>.
/// </summary>
/// <remarks>
/// This class is responsible for the *creation* (via <see cref="Add"/>)
/// and *storage* of systems.
///
/// <see cref="IGameState"/> implementations are responsible for the *execution*
/// of these systems by retrieving them via <see cref="Get{T}"/> and then
/// calling their specific method.
/// </remarks>
public class SystemManager
{
    // The maximum sub priority a system may have.
    private const int MMaxSubPriority = 100;

    public class SystemEntry<T> where T : class, ISystem
    {
        public T mSystem;
        public SystemGroup mGroup;
        public int mPriority;
        public bool mIsEnabled;
    }

    private readonly Dictionary<Type, ISystem> mRegistry = new();
    private List<SystemEntry<ISystem>> mDebugInfo = new();
    public IReadOnlyList<SystemEntry<ISystem>> DebugInfo => mDebugInfo;

    // Buckets
    private readonly List<SystemEntry<IInputSystem>> mInputBucket = [];
    private readonly List<SystemEntry<IUpdateSystem>> mUpdateBucket = [];
    private readonly List<SystemEntry<IDrawSystem>> mDrawBucket = [];

    private bool mInputDirty = false;
    private bool mUpdateDirty = false;
    private bool mDrawDirty = false;

    /// <summary>
    /// Registers a single system instance. This is typically called
    /// in <see cref="Game1.Initialize"/>.
    /// </summary>
    /// <param name="system">The system instance to store.</param>
    /// <param name="group"></param>
    /// <param name="subPriority"></param>
    /// <param name="enabled"></param>
    /// <typeparam name="T">The concrete type of the system (e.g. MovementSystem).</typeparam>
    public void Add<T>(T system, SystemGroup group, int subPriority = 0, bool enabled = true) where T : class, ISystem
    {
        if ((subPriority is < 0 or >= MMaxSubPriority) || system is not (IInputSystem or IUpdateSystem or IDrawSystem))
        {
            throw new ArgumentException($"System {typeof(T)} registration is invalid!");
        }

        var type = system.GetType();
        if (mRegistry.ContainsKey(type))
        {
            throw new ArgumentException($"System {type.Name} is already registered!");
        }

        mRegistry[type] = system;
        var priority = (int)group + subPriority;
        if (system is IInputSystem inputSystem)
        {
            var entry = new SystemEntry<IInputSystem>
            {
                mSystem = inputSystem,
                mGroup = group,
                mPriority = priority,
                mIsEnabled = true,
            };

            mInputBucket.Add(entry);
            mInputDirty = true;
        }

        if (system is IUpdateSystem updateSystem)
        {
            var entry = new SystemEntry<IUpdateSystem>
            {
                mSystem = updateSystem,
                mGroup = group,
                mPriority = priority,
                mIsEnabled = true,
            };

            mUpdateBucket.Add(entry);
            mUpdateDirty = true;
        }

        if (system is IDrawSystem drawSystem)
        {
            var entry = new SystemEntry<IDrawSystem>
            {
                mSystem = drawSystem,
                mGroup = group,
                mPriority = priority,
                mIsEnabled = true,
            };

            mDrawBucket.Add(entry);
            mDrawDirty = true;
        }

        var debugEntry = new SystemEntry<ISystem>()
        {
            mSystem = system,
            mGroup = group,
            mPriority = priority,
            mIsEnabled = true,
        };
        mDebugInfo.Add(debugEntry);
    }

    public T Get<T>() where T : class, ISystem
    {
        if (mRegistry.TryGetValue(typeof(T), out var system))
        {
            return system as T;
        }

        throw new ArgumentException($"System {typeof(T).Name} is not registered!");
    }

    public void ToggleSystem(Type systemType, bool enabled)
    {
        ToggleInBucket(mInputBucket, systemType, enabled);
        ToggleInBucket(mUpdateBucket, systemType, enabled);
        ToggleInBucket(mDrawBucket, systemType, enabled);
    }

    public void ToggleGroup(SystemGroup systemGroup, bool enabled)
    {
        ToggleBucket(mInputBucket, systemGroup, enabled);
        ToggleBucket(mUpdateBucket, systemGroup, enabled);
        ToggleBucket(mDrawBucket, systemGroup, enabled);
    }

    // --- EXECUTION ---
    public void HandleInput(World world, InputService input)
    {
        EnsureSorted();

        foreach (var entry in mInputBucket)
        {
            if (entry.mIsEnabled) { entry.mSystem.HandleInput(world, input); }
        }
    }

    public void Update(World world, GameTime gameTime)
    {
        EnsureSorted();

        foreach (var entry in mUpdateBucket)
        {
            if (entry.mIsEnabled) { entry.mSystem.Update(world, gameTime); }
        }
    }

    public void Draw(World world, SpriteBatch spriteBatch, bool ySorting=false)
    {
        EnsureSorted();

        foreach (var entry in mDrawBucket)
        {
            if (entry.mIsEnabled) { entry.mSystem.Draw(world, spriteBatch, ySorting); }
        }
    }

    // --- HELPERS ---
    private void EnsureSorted()
    {
        if (mInputDirty)
        {
            mInputBucket.Sort((a, b) => a.mPriority.CompareTo(b.mPriority));
            mInputDirty = false;
        }

        if (mUpdateDirty)
        {
            mUpdateBucket.Sort((a, b) => a.mPriority.CompareTo(b.mPriority));
            mUpdateDirty = false;
        }

        if (mDrawDirty)
        {
            mDrawBucket.Sort((a, b) => a.mPriority.CompareTo(b.mPriority));
            mDrawDirty = false;
        }
    }

    private void ToggleInBucket<TInterface>(List<SystemEntry<TInterface>> bucket, Type targetType, bool enabled) where TInterface : class, ISystem
    {
        for (var i = 0; i < bucket.Count; i++)
        {
            var runtimeType = bucket[i].mSystem.GetType();

            if (runtimeType != targetType) { continue; }
            var entry = bucket[i];
            entry.mIsEnabled = enabled;
            bucket[i] = entry;
        }

        var debugEntry = mDebugInfo.FirstOrDefault(x=> x.mSystem.GetType() == targetType);
        if (debugEntry is not null) { debugEntry.mIsEnabled = enabled; }
    }

    private void ToggleBucket<T>(List<SystemEntry<T>> bucket, SystemGroup systemGroup, bool enabled)
        where T : class, ISystem
    {
        for (var i = 0; i < bucket.Count; i++)
        {
            if (bucket[i].mGroup != systemGroup) { continue; }

            var entry = bucket[i];
            entry.mIsEnabled = enabled;
            bucket[i] = entry;
        }
    }
}