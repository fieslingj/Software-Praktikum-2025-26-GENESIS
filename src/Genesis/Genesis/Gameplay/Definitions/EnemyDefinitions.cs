using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Genesis.Gameplay.Definitions;

/// <summary>
/// Central definition of enemy types.
/// </summary>
public sealed class EnemyDefinition
{
    public EnemyType Type { get; init; }
    public bool IsMutant => Type is EnemyType.Mutant1 or EnemyType.Mutant2 or EnemyType.Mutant3;
    public string SpritePathIdle { get; init; } = "";
    public string SpritePathCorpse { get; init; } = "";
    public int SourceWidth { get; init; }
    public float LayerDepth { get; init; } = 0.1f;
    public float Scale { get; init; } = 1.0f;
    public Vector2 SpriteOriginOffsetFactor { get; init; }

    // Basic stats
    public float Speed { get; init; }
    public float MaxHealth { get; init; }
    public int Mass { get; init; }
    public float EnrageThreshold { get; init; }
    public float CautionThreshold { get; init; }

    public float MeleeDamageFactor { get; init; } = 1f;
    public float RangedDamageFactor { get; init; } = 1f;

    // Hitbox & collider
    public Vector2 ColliderSizeFactor { get; init; }
    public float HitboxHeightFactor { get; init; }
    
    // Inventory
    public IReadOnlyList<InventorySlot> Inventory { get; init; }
    public ItemType? MeleeWeapon { get; init; }
    public ItemType? RangedWeapon { get; init; }
    public int InitialAmmo { get; init; }
    public int Coins { get; init; }
    
    // Ducking
    public bool CanDuck { get; init; } = false;
    public float DuckProbability { get; init; } = 0.0f;
    public float DuckDuration { get; init; } = 0.0f;
    
    // Taking cover
    public bool CanTakeCover { get; init; } = false;
    public float CoverSearchRadius { get; init; } = 300f;
    public float CoverPreference { get; init; } = 0.5f;
    
    // Animation
    public int IdleFrames { get; init; }
    public int WalkFrames { get; init; }
    public float FrameDuration { get; init; }
    
    public int Framesize {get; init;}
}

public static class EnemyDefinitions
{
    private static readonly EnemyDefinition[] sAllEnemies =
    [
        new()
        {
            Type = EnemyType.Scientist,
            SpritePathIdle = "Sprites/Enemies/Scientist/Idle",
            SpritePathCorpse = "Sprites/Enemies/Scientist/Corpse",
            SourceWidth = 32,
            Scale = 1.2f,
            Speed = 70f,
            MaxHealth = 40f,
            Mass = 70,
            EnrageThreshold = 0.3f,
            CautionThreshold = 0.7f,
            SpriteOriginOffsetFactor = new Vector2(0f, 0.35f),
            ColliderSizeFactor = new Vector2(0.35f, 0.25f),
            HitboxHeightFactor = 0.65f,
            Inventory = [(ItemType.HealthSyringe, 1)],
            MeleeWeapon = ItemType.Fist,
            InitialAmmo = 0,
            Coins = 100,
            CanTakeCover = true,
            CoverSearchRadius = 100f,
            CoverPreference = 0.8f,
            IdleFrames = 4,
            WalkFrames = 6,
            FrameDuration = 120f,
        },
        new()
        {
            Type = EnemyType.Security,
            SpritePathIdle = "Sprites/Enemies/Security/Idle",
            SpritePathCorpse = "Sprites/Enemies/Security/Corpse",
            SourceWidth = 350,
            Scale = 1.2f,
            Speed = 85f,
            MaxHealth = 80f,
            Mass = 110,
            EnrageThreshold = 0.5f,
            CautionThreshold = 0.9f,
            SpriteOriginOffsetFactor = new Vector2(0f, 0.4f),
            ColliderSizeFactor = new Vector2(0.35f, 0.25f),
            HitboxHeightFactor = 0.6f,
            Inventory = [ItemType.Pistol, ItemType.EnergyBar],
            MeleeWeapon = ItemType.Fist,
            RangedWeapon = ItemType.Pistol,
            MeleeDamageFactor = 1.5f,
            RangedDamageFactor = 0.2f,
            InitialAmmo = 50,
            Coins = 150,
            CanTakeCover = true,
            CoverSearchRadius = 100f,
            CoverPreference = 0.8f,
            IdleFrames = 4,
            WalkFrames = 4,
            FrameDuration = 120f,
        },
        new()
        {
            Type = EnemyType.Robot,
            SpritePathIdle = "Sprites/Enemies/Robot/Idle",
            SpritePathCorpse = "Sprites/Enemies/Robot/Corpse",
            SourceWidth = 350,
            Scale = 1.2f,
            Speed = 70f,
            MaxHealth = 120f,
            Mass = 180,
            EnrageThreshold = 0.4f,
            CautionThreshold = 0.8f,
            SpriteOriginOffsetFactor = new Vector2(0f, 0.35f),
            ColliderSizeFactor = new Vector2(0.7f, 0.4f),
            HitboxHeightFactor = 0.9f,
            Inventory = [],
            RangedWeapon = ItemType.Minigun,
            Coins = 200,
            IdleFrames = 8,
            WalkFrames = 4,
            FrameDuration = 120f,
        },
        new()
        {
            Type = EnemyType.Ceo,
            SpritePathIdle = "Sprites/Enemies/CEO/Office_Guy_Animation",
            SpritePathCorpse = "Sprites/Enemies/CEO/Corpse",
            SourceWidth = 16,
            Scale = 2f,
            Speed = 90f,
            MaxHealth = 300f,
            Mass = 300,
            EnrageThreshold = 0.5f,
            CautionThreshold = 1f,
            SpriteOriginOffsetFactor = new Vector2(0f, 0.4f),
            ColliderSizeFactor = new Vector2(0.35f, 0.25f),
            HitboxHeightFactor = 0.55f,
            Inventory = [],
            MeleeWeapon = ItemType.ArmsOfSteel,
            MeleeDamageFactor = 1.5f,
            RangedDamageFactor = 1.5f,
            Coins = 200,
            CanDuck = true,
            DuckProbability = 0.25f,
            DuckDuration = 2f,
            IdleFrames = 4,
            WalkFrames = 4,
            FrameDuration = 120f,
            Framesize = 32
        },
        new()
        {
            Type = EnemyType.Mutant1,
            SpritePathIdle = "Sprites/Mutants/laserarm",
            SpritePathCorpse = "Sprites/Mutants/laserarm_corpse",
            SourceWidth = 16,
            Scale = 1.8f,
            Speed = 40f,
            MaxHealth = 100f,
            Mass = 120,
            EnrageThreshold = 0.5f,
            CautionThreshold = 1f,
            SpriteOriginOffsetFactor = new Vector2(0f, 0.4f),
            ColliderSizeFactor = new Vector2(0.3f, 0.2f),
            HitboxHeightFactor = 0.55f,
            Inventory = [ItemType.LaserArmSyringe],
            RangedWeapon = ItemType.LaserArm,
            Coins = 200,
            CanDuck = true,
            DuckProbability = 0.25f,
            DuckDuration = 2f,
            IdleFrames = 4,
            WalkFrames = 4,
            FrameDuration = 120f,
            Framesize = 32
        },
        new()
        {
            Type = EnemyType.Mutant2,
            SpritePathIdle = "Sprites/Mutants/acidspitter",
            SpritePathCorpse = "Sprites/Mutants/acidspitter_corpse",
            SourceWidth = 16,
            Scale = 1.8f,
            Speed = 40f,
            MaxHealth = 100f,
            Mass = 100,
            EnrageThreshold = 0.5f,
            CautionThreshold = 1f,
            SpriteOriginOffsetFactor = new Vector2(0f, 0.4f),
            ColliderSizeFactor = new Vector2(0.3f, 0.2f),
            HitboxHeightFactor = 0.55f,
            Inventory = [ItemType.AcidSpitSyringe],
            RangedWeapon = ItemType.AcidSpit,
            Coins = 200,
            CanDuck = true,
            DuckProbability = 0.25f,
            DuckDuration = 2f,
            IdleFrames = 4,
            WalkFrames = 4,
            FrameDuration = 120f,
            Framesize = 32
        },
        new()
        {
            Type = EnemyType.Mutant3,
            SpritePathIdle = "Sprites/Mutants/armsofsteel",
            SpritePathCorpse = "Sprites/Mutants/armsofsteel_corpse",
            SourceWidth = 16,
            Scale = 1.8f,
            Speed = 40f,
            MaxHealth = 100f,
            Mass = 250,
            EnrageThreshold = 0.5f,
            CautionThreshold = 1f,
            SpriteOriginOffsetFactor = new Vector2(0f, 0.4f),
            ColliderSizeFactor = new Vector2(0.3f, 0.2f),
            HitboxHeightFactor = 0.55f,
            Inventory = [ItemType.ArmsOfSteelSyringe],
            MeleeWeapon = ItemType.ArmsOfSteel,
            Coins = 200,
            CanDuck = true,
            DuckProbability = 0.25f,
            DuckDuration = 2f,
            IdleFrames = 4,
            WalkFrames = 4,
            FrameDuration = 120f,
            Framesize = 32
        }
    ];

    private static readonly Dictionary<EnemyType, EnemyDefinition> sDefinitions = 
        sAllEnemies.ToDictionary(def => def.Type);

    public static EnemyDefinition Get(EnemyType type)
    {
        return sDefinitions.GetValueOrDefault(type, sDefinitions[EnemyType.Scientist]);
    }
}

public record struct InventorySlot(ItemType Type, int Amount = 1)
{
    public static implicit operator InventorySlot((ItemType type, int amount) tuple)
    {
        return new InventorySlot(tuple.type, tuple.amount);
    }

    public static implicit operator InventorySlot(ItemType type)
    {
        return new InventorySlot(type, 1);
    }
}