using System;

namespace Genesis.Gameplay.Components;

/// <summary>
/// The current and maximal health of an entity.
/// </summary>
[Serializable]
public struct HealthComponent(float max, float? initial = null)
{
    public float Max { get; set; } = max;
    public float Current { get; set; } = initial ?? max;
}