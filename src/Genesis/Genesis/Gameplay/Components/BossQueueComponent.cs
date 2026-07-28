using System;
using System.Collections.Generic;
using Genesis.Gameplay.Definitions;

namespace Genesis.Gameplay.Components;

[Serializable]
public struct BossQueueComponent(List<EnemyType> remainingBosses)
{
    public List<EnemyType> RemainingBosses { get; set; } = remainingBosses;
}