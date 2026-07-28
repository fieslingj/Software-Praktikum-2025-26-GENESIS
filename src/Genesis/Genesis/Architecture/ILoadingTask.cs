using Arch.Core;
using Genesis.Architecture.Audio;
using Microsoft.Xna.Framework.Content;

namespace Genesis.Architecture;

/// <summary>
/// Defines a loading strategy to be,
/// executed by the <see cref="GameStates.LoadingState"/>.
/// </summary>
public interface ILoadingTask
{
    /// <summary>
    /// Executes the full loading process of a new world. (synchronous)
    /// </summary>
    /// <param name="world">Reference to the ECS world.</param>
    /// <param name="contentManager">The ContentManager to load assets from.</param>
    /// <param name="mapLoader">The MapLoader to load Tiled maps.</param>
    /// <param name="audioService">The SoundManager to load SFX instances.</param>
    void Execute(World world, ContentManager contentManager, MapLoader mapLoader, AudioService audioService);
}