using Genesis.Gameplay.Definitions;

namespace Genesis.Gameplay.Components;

/// <summary>
/// Scales the damange of attacker based on the attack type.
/// </summary>
public struct AttackScalingComponent(float meleeMultiplier, float rangedMultiplier)
{
    /// <summary>
    /// Mulitiplicator for meele attacks
    /// </summary>
    public float MeleeMultiplier { get; set; } = meleeMultiplier;

    /// <summary>
    /// Mulitiplicator for ranged attacks
    /// </summary>
    public float RangedMultiplier { get; set; } = rangedMultiplier;

    /// <summary>
    /// Apllies the scaling to the base damage based on the attack type.
    /// </summary>
    public float Apply(float baseDamage, ItemAttackType attackType)
    {
        return attackType switch
        {
            ItemAttackType.Melee => baseDamage * MeleeMultiplier,
            ItemAttackType.Ranged => baseDamage * RangedMultiplier,
            _ => baseDamage
        };
    }
}