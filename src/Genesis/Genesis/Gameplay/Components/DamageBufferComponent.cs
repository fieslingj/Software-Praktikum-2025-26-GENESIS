using System.Runtime.CompilerServices;
using Arch.Core;

namespace Genesis.Gameplay.Components;

/// <summary>
/// A transient component that accumulates all damage events received by an entity
/// during a single frame. It is cleared automatically at the end of the frame.
/// </summary>
/// <remarks>
/// <para>
/// <b>Performance Note:</b> This component is a "Zero-Allocation Ring Buffer".
/// It uses an inline array to store hits directly in chunk memory, avoiding the
/// overhead of <c>List&lt;T&gt;</c> or array resizing.
/// </para>
/// <para>
/// <b>Usage:</b> Do not add or modify this component manually.
/// Always use the <see cref="Genesis.Gameplay.Extensions.CombatExtensions.InflictDamage"/>
/// extension method to ensure thread safety and overflow protection.
/// </para>
/// </remarks>
public struct DamageBufferComponent
{
    public const int MaxHits = 8;
    
    public DamagePayloadBuffer mHits;
    public int HitsCount { get; set; }
}

/// <summary>
/// Represents a single damage event (hit) received by an entity.
/// </summary>
/// <param name="value">The raw amount of damage to apply.</param>
/// <param name="source">The entity responsible for dealing damage.</param>
public readonly struct DamagePayload(float value, Entity source)
{
    /// <summary>
    /// The raw amount of damage to apply.
    /// </summary>
    public float Value { get; } = value;
    
    /// <summary>
    /// The entity responsible for dealing damage.
    /// Useful for tracking statistics.
    /// </summary>
    public Entity Source { get; } = source;
}

/// <summary>
/// Internal storage struct for the <see cref="DamageBufferComponent"/>.
/// </summary>
/// <remarks>
/// This uses the .NET 8 <see cref="InlineArrayAttribute"/> to create a fixed-size,
/// contiguous block of memory within the parent struct. This behaves like an array
/// but requires zero heap allocation and zero garbage collection.
/// </remarks>
[InlineArray(DamageBufferComponent.MaxHits)]
public struct DamagePayloadBuffer
{
    private DamagePayload Element0 { get; set; }
}
