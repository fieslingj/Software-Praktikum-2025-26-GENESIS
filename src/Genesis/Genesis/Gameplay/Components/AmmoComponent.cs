using System;

namespace Genesis.Gameplay.Components;

[Serializable]
public struct AmmoComponent(int currentAmount)
{
    public int Current { get; set; } = currentAmount;
}

