using System;
using System.Collections.Generic;
using System.Linq;

namespace Genesis.Gameplay.Components;

[Serializable]
public struct BloodlustTrackerComponent()
{
    public Queue<(double TimeStamp, float Damage)> HitBuffer { get; } = new();

    public bool IsUnlocked { get; set; } = false;
    public bool HasGrantedAchievement { get; set; } = false;
    public const double WindowDuration = 20.0;
    public const float DamageTarget = 200.0f;
    
    public float CurrentDamageSum(double currentTime) => HitBuffer
        .Where(x => x.TimeStamp > currentTime - WindowDuration)
        .Sum(x => x.Damage);
}