using Genesis.Gameplay.Entities;

namespace Genesis.Architecture;

//Fabriken Sammlung
public class FactoryService(ProjectileEntity projectiles, ExplosivesFactory explosives, EffectFactory effects, EnemyFactory enemies, MinigunFactory miniguns)
{
    public ProjectileEntity MProjectileFactory { get; } = projectiles;
    public ExplosivesFactory MExplosivesFactory { get; } = explosives;
    public EffectFactory MEffectFactory { get; } = effects;

    public EnemyFactory MEnemyFactory { get; } = enemies;
    public MinigunFactory MMinigunFactory { get; } = miniguns;
}