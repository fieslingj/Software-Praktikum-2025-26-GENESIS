using System;
using Arch.Core;
using Genesis.Gameplay.Definitions;

namespace Genesis.Gameplay.Components;

/// <summary>
/// Identifies the entity as a companion and defines its specific type.
/// It also stores the current target entity.
/// </summary>
[Serializable]
public struct CompanionComponent(EnemyType type)
{
    public EnemyType Type { get; set; } = type;
    public Entity TargetEntity { get; set; } = Entity.Null;
}