using System;

namespace Genesis.Gameplay.Components;

[Serializable]
public enum MutantType
{
    Mutant1,
    Mutant2,
    Mutant3
}

[Serializable]
public struct MutantTypeComponent(MutantType type)
{
    public MutantType Type { get; set; } = type;
}