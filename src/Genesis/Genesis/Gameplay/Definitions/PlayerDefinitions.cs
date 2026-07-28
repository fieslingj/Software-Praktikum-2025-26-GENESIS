using System;
using System.Collections.Generic;
using System.Linq;
using Genesis.Gameplay.Components;

namespace Genesis.Gameplay.Definitions;

public sealed class PlayerDefinition
{
    public MutantType Type { get; init; }
    public string Name { get; init; }
    public string Description { get; init; } = "No Description available";
    public float MovementSpeed { get; init; }
    public int MaxHealth { get; init; }
    public int MaxStamina { get; init; }
    public int Mass { get; init; }
    public string SpritePath { get; init; } = "";
    public float Scale { get; init; }
    public ItemType SpecialItem { get; init; }
}

public static class PlayerDefinitions
{
    private static readonly PlayerDefinition[] sAllPlayers =
    [
        new()
        {
            Type = MutantType.Mutant1,
            Name = "Laser Arm",
            Description = "A versatile all-rounder excelling in adaptability with balanced speed and durability.",
            MovementSpeed = 100f,
            MaxHealth = 100,
            MaxStamina = 100,
            Mass = 100,
            SpritePath = "Sprites/Mutants/laserarm",
            Scale = 1.2f,
            SpecialItem = ItemType.LaserArm
        },
        new()
        {
            Type = MutantType.Mutant2,
            Name = "Acid Spitter",
            Description = "A high-mobility skirmisher designed for hit-and-run tactics.",
            MovementSpeed = 120f,
            MaxHealth = 80,
            MaxStamina = 120,
            Mass = 70,
            SpritePath = "Sprites/Mutants/acidspitter",
            Scale = 1.2f,
            SpecialItem = ItemType.AcidSpit
        },
        new()
        {
            Type = MutantType.Mutant3,
            Name = "Arms of Steel",
            Description = "A resilient juggernaut that dominates through sheer endurance.",
            MovementSpeed = 70f,
            MaxHealth = 160,
            MaxStamina = 80,
            Mass = 250,
            SpritePath = "Sprites/Mutants/armsofsteel",
            Scale = 1.2f,
            SpecialItem = ItemType.ArmsOfSteel
        }
    ];
    
    private static readonly Dictionary<MutantType, PlayerDefinition> sDefinitions = 
        sAllPlayers.ToDictionary(def => def.Type);

    public static PlayerDefinition Get(MutantType type)
    {
        if (sDefinitions.TryGetValue(type, out var def))
        {
            return def;
        }
        
        // Fallback / Default
        return new PlayerDefinition
        {
            Type = type,
            MovementSpeed = 100f,
            MaxHealth = 100,
            MaxStamina = 100,
            Mass = 100,
            SpritePath = "Sprites/Mutants/Base",
            Scale = 1.2f,
            SpecialItem = ItemType.None
        };
    }

    public static EnemyDefinition GetMutantEnemyDefinition(MutantType type)
    {
        return type switch
        {
            MutantType.Mutant1 => EnemyDefinitions.Get(EnemyType.Mutant1),
            MutantType.Mutant2 => EnemyDefinitions.Get(EnemyType.Mutant2),
            MutantType.Mutant3 => EnemyDefinitions.Get(EnemyType.Mutant3),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}