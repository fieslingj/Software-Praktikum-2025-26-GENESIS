using System;
using Arch.Core;

namespace Genesis.Gameplay.Extensions;

/// <summary>
/// Provides extension methods to emulate "Resources" (singleton data) in Arch ECS, inspired by Bevy ECS.
/// </summary>
/// <remarks>
/// <para>
/// In Bevy ECS, a "Resource" is a singleton struct of data that is globally accessible and mutable.
/// Arch does not have a built-in concept for this.
/// </para>
/// <para>
/// These extensions define a "Resource" as a Component that exists on an Entity which holds *only* that single Component.
/// This effectively creates a singleton container for that data type.
/// </para>
/// <para>
/// <b>Important Distinction:</b> Unlike Bevy Resources, these Arch-based Resources are tied to the World's lifecycle.
/// They are <b>not persistent</b> across <c>World.Clear()</c>. When the world is cleared, these resources are destroyed
/// and must be re-initialized/reset.
/// </para>
/// </remarks>
public static class ResourceExtensions
{
    /// <summary>
    /// Retrieves a Resource (singleton Component) of type <typeparamref name="T"/> from the World.
    /// </summary>
    /// <typeparam name="T">The type of the Resource (Component) to retrieve.</typeparam>
    /// <param name="world">The Arch World instance.</param>
    /// <returns>The instance of the Resource <typeparamref name="T"/>.</returns>
    /// <remarks>
    /// This method assumes the Resource exists. It searches for the first entity that has *exclusively* this component.
    /// </remarks>
    public static T GetResource<T>(this World world)
    {
        var entity = world.GetFirstEntity(new QueryDescription().WithExclusive<T>());
        if (entity == Entity.Null)
        {
            return default(T);
        }

        return world.Get<T>(entity);
    }

    public static bool TryGetResource<T>(this World world, out T resource)
    {
        var entity = world.GetFirstEntity(new QueryDescription().WithExclusive<T>());
        if (entity == Entity.Null)
        {
            resource = default(T);
            return false;
        }
        resource = world.Get<T>(entity);
        return true;
    }

    /// <summary>
    /// Sets or creates a Resource (singleton Component) of type <typeparamref name="T"/> in the World.
    /// </summary>
    /// <typeparam name="T">The type of the Resource (Component) to set.</typeparam>
    /// <param name="world">The Arch World instance.</param>
    /// <param name="resource">The data for the Resource.</param>
    /// <returns>The Entity holding the Resource.</returns>
    /// <remarks>
    /// If an entity with exclusively this component already exists, its value is updated.
    /// If not, a new entity is created to hold this Resource.
    /// </remarks>
    public static Entity SetResource<T>(this World world, T resource)
    {
        var entity = world.GetFirstEntity(new QueryDescription().WithExclusive<T>());
        if (entity == Entity.Null) { entity = world.Create(resource); }
        else { world.Set(entity, resource); }
        return entity;
    }
}