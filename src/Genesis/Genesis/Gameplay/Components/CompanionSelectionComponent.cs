using Arch.Core;

namespace Genesis.Gameplay.Components;

public struct CompanionSelectionComponent(Entity companion)
{
    public Entity Companion { get; set; } = companion;
}