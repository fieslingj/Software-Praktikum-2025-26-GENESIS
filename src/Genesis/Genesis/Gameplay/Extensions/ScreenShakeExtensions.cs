using Arch.Core;
using Genesis.Gameplay.Components;

namespace Genesis.Gameplay.Extensions;

public static class ScreenShakeExtensions
{
    /// <summary>
    /// Spawns a new ShakeSource entity with the specified parameters.
    /// </summary>
    /// <param name="world">The ECS World.</param>
    /// <param name="trauma">Initial trauma (0.0 to 1.0).</param>
    /// <param name="decay">Trauma decay per second.</param>
    /// <param name="maxOffset">Maximum pixel offset at full trauma.</param>
    /// <param name="maxRotation">Maximum rotation in radians at full trauma.</param>
    public static void ApplyScreenShake(this World world, float trauma, float decay = 1.0f, float maxOffset = 15f, float maxRotation = 0.1f)
    {
        world.Create(new ShakeSourceComponent(trauma, decay, maxOffset, maxRotation));
    }

    /// <summary>
    /// Spawns a small, quick screen shake. Good for minor impacts.
    /// </summary>
    public static void ShakeSmall(this World world)
    {
        world.ApplyScreenShake(0.3f, 2.0f, 5f, 0.05f);
    }

    /// <summary>
    /// Spawns a medium screen shake. Good for standard explosions or heavy hits.
    /// </summary>
    public static void ShakeMedium(this World world)
    {
        world.ApplyScreenShake(0.6f, 1.5f, 15f, 0.1f);
    }

    /// <summary>
    /// Spawns a large, long-lasting screen shake. Good for massive events or boss deaths.
    /// </summary>
    public static void ShakeLarge(this World world)
    {
        world.ApplyScreenShake(1.0f, 0.8f, 30f, 0.2f);
    }
}