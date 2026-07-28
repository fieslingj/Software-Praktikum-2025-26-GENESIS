using System.Collections.Generic;
using Arch.Core;
using Genesis.Gameplay.Definitions;

namespace Genesis.Gameplay.Components;

public class AreaOfEffectComponent(float radius, float damage,List<Entity> spriteEffect, List<StatusType>  statusEffectList, string soundPath = null)
{
    public float Radius { get; } = radius;
    public float Damage { get; } = damage;
    public List<Entity> SpriteEffects { get; } = spriteEffect;
    public List<StatusType> StatusEffects { get; } = statusEffectList;
    public string SoundPath { get; } = soundPath;
}