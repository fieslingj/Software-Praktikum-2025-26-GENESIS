using System;
using Arch.Core;
using Genesis.Gameplay.Definitions;

namespace Genesis.Gameplay.Components;

/// <summary>
/// Identifies the entity as an enemy and defines its specific type.
/// </summary>
[Serializable]
public struct EnemyComponent(EnemyType type)
{
    public EnemyType Type { get; init; } = type;
    public Entity TargetEntity { get; set; } = Entity.Null;
}