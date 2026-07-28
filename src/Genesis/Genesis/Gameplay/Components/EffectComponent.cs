namespace Genesis.Gameplay.Components;

public enum EffectType
{
    ExplosionAnimation,
    SmokeWaveAnimationSmall,
    LeakingChemicalTank,
}

public struct EffectComponent(EffectType type, bool active = false)
{
    public EffectType Type { get; init; } = type;
    public bool Active { get; set; } = active;
}