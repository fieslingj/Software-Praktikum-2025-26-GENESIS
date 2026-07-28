using System;
using Genesis.Gameplay.Definitions;

namespace Genesis.Gameplay.Components;

/// <summary>
/// Identifies the entity as an enemy corpse and defines its specific enemy type.
/// </summary>
[Serializable]
public readonly struct CorpseComponent
{
    public EnemyType Type { get; init; }

    public CorpseComponent(EnemyType type)
    {
        Type = type;
    }
}