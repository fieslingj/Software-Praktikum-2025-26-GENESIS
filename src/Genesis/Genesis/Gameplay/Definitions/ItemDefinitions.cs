using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Genesis.Gameplay.Definitions;

public enum ItemType
{
    None = 0,
    EnergyBar,
    HealthSyringe,
    StunGrenade,
    RemoteExplosive,
    Minigun,
    Shield,
    Neurochip,
    LaserArm,
    AcidSpit,
    ArmsOfSteel,
    Pipe,
    Pistol,
    Fist,
    LaserArmSyringe,
    AcidSpitSyringe,
    ArmsOfSteelSyringe,
    Munition
}

public enum ItemAttackType
{
    None,
    Melee,
    Ranged
}

public sealed class ItemDefinition
{
    public ItemType Type { get; init; }

    // UI Info
    public string Name { get; init; } = "Unknown Item";
    public string Description { get; init; } = "No description available.";

    // Shop / Inventory
    public int Price { get; init; }
    public string IconPath { get; init; } = "Sprites/Icons/Default";
    public bool Stackable { get; init; } = true;

    // Consumable Stats
    public float HealthRestore { get; init; }
    public float StaminaRestore { get; init; }
    public bool IsConsumable => HealthRestore > 0 || StaminaRestore > 0;

    // Combat Stats
    public ItemAttackType AttackType { get; init; } = ItemAttackType.None;
    public float Damage { get; init; }
    public float AttackRange { get; init; }
    public float Cooldown { get; init; }
    public float ProjectileLifeTime { get; init; }
    public bool UsesAmmo {get; init;}

    public Vector2 HitboxSize { get; init; }

    public float ProjectileSpeed { get; init; }
    //Path zum Projectile Sprite
    public string ProjectileSpritePath { get; init; }

    public float ProjectileSpriteScale { get; init; } = 0.02f;
    //Projectile animation
    public int Frames { get; init; }

    public float FrameDuration { get; init; }

    public int Framewidth {get; init;}
    public int Frameheight {get; init;}

    //sound
    public string WeaponUseSoundPath  { get; init; }

    //AOE Stats
    public float AoeDamage { get; init; }

    public float AoeRange { get; init; }
    public StatusType AoeStatusEffect { get; init; }

    // Shield
    public int Durability { get; init; }
}

public static class ItemDefinitions
{
    private static readonly ItemDefinition sDefault = new();

    private static readonly ItemDefinition[] sAllItems =
    [
        new() {
            Type = ItemType.EnergyBar,
            Name = "Energy Bar",
            Description = "A high-calorie snack that immediately restores 50 Stamina. Use it to keep sprinting or dodging when exhausted.",
            Price = 50,
            StaminaRestore = 50f,
            IconPath = "Sprites/Icons/EnergyBar"
        },
        new() {
            Type = ItemType.HealthSyringe,
            Name = "Health Syringe",
            Description = "Injects advanced medical nanobots to immediately restore 50 Health points.",
            Price = 50,
            HealthRestore = 50f,
            IconPath = "Sprites/Icons/HealthSyringe"
        },
        new() {
            Type = ItemType.StunGrenade,
            Name = "Stun Grenade",
            Description = "A throwable tactical device that confuses and immobilizes all enemies in the blast area.",
            Price = 100,
            IconPath = "Sprites/Icons/StunGrenade",
            AttackType = ItemAttackType.Ranged,
            AttackRange = 300f,
            ProjectileSpeed = 250f,
            ProjectileLifeTime = 2.0f,
            AoeRange = 50f,
            AoeDamage = 1f,
            AoeStatusEffect = StatusType.Stunned
        },
        new() {
            Type = ItemType.RemoteExplosive,
            Name = "Remote Explosive",
            Description = "Deploy as many charges as you like. Once placed, press [F] to trigger a massive, simultaneous detonation.",
            Price = 100,
            IconPath = "Sprites/Icons/RemoteExplosive",
            AttackType = ItemAttackType.Ranged,
            AttackRange = 100f,
            ProjectileSpeed = 200f,
            AoeRange = 50f,
            AoeDamage = 40f
        },
        new() {
            Type = ItemType.Minigun,
            Name = "Minigun",
            Description = "A deployable stationary turret that automatically targets nearby enemies with a high rate of fire.",
            Price = 500,
            IconPath = "Sprites/Icons/Minigun",
            WeaponUseSoundPath = "Sounds/Attack/22LR Single",
            AttackType = ItemAttackType.Ranged,
            Damage = 2f,
            Cooldown = 0.1f,
            AttackRange = 300f,
            ProjectileSpeed = 600f
        },
        new() {
            Type = ItemType.Shield,
            Name = "Shield",
            Description = "Blocks incoming attacks from the player's facing direction. The shield is destroyed after absorbing 3 hits.",
            Price = 300,
            IconPath = "Sprites/Icons/Shield",
            Durability = 3
        },
        new()
        {
            Type = ItemType.Neurochip,
            Name = "Neurochip",
            Description = "Implant this into a weakened mutant enemy (indicated by a purple glow) that has been stunned by a grenade. " +
                          "Once implanted at close range, the mutant becomes a permanent companion.",
            Price = 1000,
            IconPath = "Sprites/Icons/Neurochip"
        },
        new()
        {
            Type = ItemType.Pipe,
            Name = "Rusty Pipe",
            Description = "A crude, low-damage provisional starting weapon to use when no professional weaponry is at hand.",
            IconPath = "Sprites/Icons/Pipe",
            WeaponUseSoundPath = "Sounds/Attack/Pipe",
            AttackType = ItemAttackType.Melee,
            Damage = 5f,
            Cooldown = 0.5f,
            ProjectileLifeTime = 0.2f,
            AttackRange = 40f,
            Stackable = false
        },
        new()
            {
            Type = ItemType.Pistol,
            Name = "Pistol",
            Description = "A reliable mid-range firearm dealing moderate damage that needs ammunition.",
            IconPath = "Sprites/Icons/Pistol",
            WeaponUseSoundPath = "Sounds/Attack/556 Single",
            AttackType = ItemAttackType.Ranged,
            Damage = 10f,
            Cooldown = 0.5f,
            AttackRange = 300f,
            ProjectileSpeed = 400f,
            UsesAmmo = true,
            Stackable = false
            },
        new()
        {
            Type = ItemType.Fist,
            Name = "Fist",
            WeaponUseSoundPath = "Sounds/Attack/FistPunch",
            AttackType = ItemAttackType.Melee,
            Damage = 2f,
            Cooldown = 0.3f,
            ProjectileLifeTime = 0.1f,
            AttackRange = 30f,
            Stackable = false
        },
        new()
        {
            Type = ItemType.ArmsOfSteel,
            Name = "Arms of Steel",
            Description = "Reinforced mechanical gauntlets that provide an increased strike rate.",
            IconPath = "Sprites/Icons/Iron",
            WeaponUseSoundPath = "Sounds/Attack/SteelFistPunch",
            AttackType = ItemAttackType.Melee,
            Damage = 5f,
            Cooldown = 0.1f,
            AttackRange = 40f,
            ProjectileLifeTime = 0.1f,
            Stackable = false
        },
        new()
        {
            Type = ItemType.AcidSpit,
            Name = "Acid Spit",
            Description = "A biological ranged attack that infects hit enemies with acid, dealing continuous corrosive damage over time.",
            IconPath = "Sprites/Icons/AcidSpit",
            ProjectileSpritePath = "Sprites/Weapons/AcidSpitProjectile",
            WeaponUseSoundPath = "Sounds/Attack/AcidShot",
            AttackType = ItemAttackType.Ranged,
            Damage = 6f,
            Cooldown = 1f,
            AttackRange = 500f,
            ProjectileSpeed = 400f,
            AoeDamage = 3f,
            AoeRange = 100f,
            AoeStatusEffect = StatusType.AcidSour,
            Stackable = false
        },
        new()
        {
            Type = ItemType.LaserArm,
            Name = "Laser Arm",
            Description = "A precise, high-tech projectile strike with infinite range and moderate damage output.",
            IconPath = "Sprites/Icons/LaserArm",
            ProjectileSpritePath = "Sprites/Weapons/LaserShot",
            WeaponUseSoundPath = "Sounds/Attack/LaserShot",
            AttackType = ItemAttackType.Ranged,
            Damage = 25f,
            Cooldown = 1f,
            AttackRange = 9999f,
            ProjectileSpeed = 600f,
            Stackable = false,
            Frames = 7,
            FrameDuration = 30f,
            Frameheight = 64,
            Framewidth = 64,
            ProjectileSpriteScale = 0.25f,
            HitboxSize = new Vector2(16f, 4f)
        },
        new()
        {
            Type = ItemType.ArmsOfSteelSyringe,
            Name = "Arms of Steel Syringe",
            Description = "A syringe that temporarily enables the Arms of Steel ability upon injection.",
            IconPath = "Sprites/Icons/SteelFistsSyringe",
            Stackable = false
        },
        new()
        {
            Type = ItemType.LaserArmSyringe,
            Name = "Laser Arm Syringe",
            Description = "A syringe that temporarily enables the Laser Arm ability upon injection.",
            IconPath = "Sprites/Icons/LaserArmSyringe",
            Stackable = false
        },
        new()
        {
            Type = ItemType.AcidSpitSyringe,
            Name = "Acid Spit Syringe",
            Description = "A syringe that temporarily enables the Acid Spit ability upon injection.",
            IconPath = "Sprites/Icons/AcidSpitSyringe",
            Stackable = false
        },
        new()
        {
            Type = ItemType.Munition,
            Name = "10 Ammunition",
            Description = "Ammunition is required for the Pistol.",
            IconPath = "Sprites/Icons/Projectile_zugeschnitten",
            Price = 100
        },
    ];

    private static readonly Dictionary<ItemType, ItemDefinition> sDefinitions =
        sAllItems.ToDictionary(def => def.Type);

    public static ItemDefinition Get(ItemType type) => sDefinitions.GetValueOrDefault(type, sDefault);
    public static int GetPrice(ItemType type) => Get(type).Price;
    public static IEnumerable<ItemType> GetShopItems()
    {
        return sDefinitions.Values
            .Where(def => def.Price > 0)
            .Select(def => def.Type);
    }
}