using Genesis.Architecture;
using Genesis.Architecture.Audio;
using Genesis.Architecture.Persistence;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Extensions;
using Genesis.GameStates.Gameplay;
using Genesis.Persistence.Meta;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Genesis.GameStates.Core;

public class LoadingState(ILoadingTask loadingTask, GraphicsDevice graphics, bool loadTechDemo = false) : IGameState
{
    private GameStateManager mGameStateManager;
    private GameServices mServices;
    private AudioService mSound;
    private MapLoader mMapLoader;

    private bool mTaskIsDone = false;

    public void Initialize(GameStateManager manager, GameServices services, ScreenService screen, AudioService sound)
    {
        mGameStateManager = manager;
        mServices = services;
        mSound = sound;
        mMapLoader = new MapLoader(services.World, services.Content, sound, graphics);
    }
    public void Enter()
    {
        var metaData = SaveManager.LoadMeta();

        mServices.World.SetResource(new MetaDataComponent(metaData));
        
        loadingTask.Execute(mServices.World, mServices.Content, mMapLoader, mSound);
        mTaskIsDone = true;
    }

    public void Exit()
    {}

    public void Pause()
    {}
    
    public void Resume()
    {}

    public void HandleInput(InputService input)
    {}

    public void Update(GameTime gameTime)
    {
        if (!mTaskIsDone)
        {
            return;
        }

        IGameState nextState = loadTechDemo ? new TechDemoState() : new IngameState();
        mGameStateManager.ChangeState(nextState);
    }
    
    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {}
}