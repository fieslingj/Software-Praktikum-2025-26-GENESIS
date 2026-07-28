using Arch.Core;
using Genesis.Architecture.ECS;
using Genesis.Persistence.Meta;
using Microsoft.Xna.Framework.Content;
using Genesis.Simulation.Achievements;

namespace Genesis.Architecture;

/// <summary>
/// A Service Locator, that holds all global services required by
/// any <see cref="IGameState"/> to work.
/// This object is created once in <see cref="Game1"/> and passed to states.
/// </summary>
public class GameServices(
    World world,
    SystemManager systemManager,
    ContentManager contentManager,
    IUiFactory uiFactory,
    ItemAssetService itemAssets,
    InputService input,
    MetaData metaData
    )
{
    public World World { get; } = world;
    public SystemManager Systems { get; } = systemManager;
    public ContentManager Content { get; } = contentManager;
    public IUiFactory UiFactory { get; set; } = uiFactory;
    public ItemAssetService ItemAssets { get; } = itemAssets;
    public InputService InputService { get; } = input;
    public MetaData MetaData { get; } = metaData;
}