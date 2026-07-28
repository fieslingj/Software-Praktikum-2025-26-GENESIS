using System;

namespace Genesis.Gameplay.Components;

/// <summary>
/// The current and maximal stamina of an entity.
/// </summary>
[Serializable]
public struct StaminaComponent(float max, float? initial = null)
{
    public float Max { get; set; } = max;
    public float Current { get; set; } = initial ?? max;
}