namespace Genesis.Gameplay.Components;

/// <summary>
/// The special ability of the first mutant.
/// </summary>
public readonly struct LaserArmComponent(float damage = 25f)
{
    public float Damage { get; } = damage;
}