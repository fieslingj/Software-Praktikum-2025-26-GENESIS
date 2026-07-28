using System;

namespace Genesis.Gameplay.Components.Purchase;

[Serializable]
public struct CoinsComponent(int currentAmount)
{
    public int CurrentAmount { get; set; } = currentAmount;
}