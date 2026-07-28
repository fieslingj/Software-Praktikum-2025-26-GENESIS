using Arch.Core;

namespace Genesis.Gameplay.Components;

public enum TrapType
{
    Bomb
}

/// <summary>
/// Makes entity a trap that triggers in a specified radius, triggering an effect-entity and dealing damage
/// </summary>
public class TrapComponent(TrapType type, float damage, Entity effect, float radius = 20)
{
    public TrapType Type { get; } = type;
    public bool IsActive { get; set; } = true;
    public float Radius { get; } = radius;
    public Entity EffectEntity { get; } = effect;
    public float Damage { get; } = damage;
}