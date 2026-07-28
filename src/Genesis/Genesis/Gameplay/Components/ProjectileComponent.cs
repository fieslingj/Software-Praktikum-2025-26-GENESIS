using Arch.Core;
using Microsoft.Xna.Framework.Audio;

namespace Genesis.Gameplay.Components;

public enum ProjectileType
{
    Bullet,
    Grenade,
    Laser,
    RemoteExplosive
}
public struct ProjectileComponent(float damage, Entity owner, double lifeTimeSeconds, string missSoundPath, bool destroyOnHit, ProjectileType type, bool sourceIsEnemy, bool nahkampf = false)
{
    public float Damage { get; } = damage;
    public Entity Source { get; } = owner;
    public double LifeTimeSeconds { get; set; } = lifeTimeSeconds;
    public string MissSoundPath { get; } = missSoundPath;
    public bool DestroyOnHit { get; } = destroyOnHit;
    public ProjectileType Type { get; } = type;
    
    public bool Nahkampf {get;} = nahkampf;
    
    // The owner entity can die before the projectile entity,
    // so we need to remember if he was an enemy or friendly for the 'no friendly fire' logic.
    public bool SourceIsEnemy { get; } = sourceIsEnemy;
}