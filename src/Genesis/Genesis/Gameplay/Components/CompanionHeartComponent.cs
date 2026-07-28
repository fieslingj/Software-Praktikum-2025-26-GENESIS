using Arch.Core;

namespace Genesis.Gameplay.Components.Visuals;

public struct CompanionHeartComponent(Entity owner)
{
    // The companion entity this heart belongs to
    public Entity Owner { get; } = owner;
}