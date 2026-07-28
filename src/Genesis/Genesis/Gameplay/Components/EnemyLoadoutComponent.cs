using Genesis.Gameplay.Definitions;

namespace Genesis.Gameplay.Components;

/// <summary>
/// Holds references to the definitions of the weapons an entity owns.
/// The AI system uses this to determine attack range and behavior.
/// </summary>
public readonly struct LoadoutComponent(ItemDefinition melee, ItemDefinition ranged)
{
    /// <summary>
    /// The definition for the close-range weapon (e.g., Fist).
    /// Can be null if the enemy has no melee attack.
    /// </summary>
    public ItemDefinition Melee { get; } = melee;

    /// <summary>
    /// The definition for the long-range weapon (e.g., Pistol).
    /// Can be null if the enemy has no ranged attack.
    /// </summary>
    public ItemDefinition Ranged { get; } = ranged;

    /// <summary>
    /// Returns true if a melee weapon is equipped.
    /// </summary>
    public bool HasMelee => Melee != null;

    /// <summary>
    /// Returns true if a ranged weapon is equipped.
    /// </summary>
    public bool HasRanged => Ranged != null;
}