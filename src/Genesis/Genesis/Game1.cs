using System;
using Genesis.Architecture;
using Genesis.Architecture.Audio;
using Genesis.Architecture.Debug;
using Genesis.Architecture.ECS;
using Genesis.Architecture.Persistence;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Entities;
using Genesis.Gameplay.Extensions;
using Genesis.Gameplay.Navigation;
using Genesis.Gameplay.Systems;
using Genesis.Gameplay.Systems.Render;
using Genesis.Gameplay.Systems.UI;
using Genesis.GameStates.Menus;
using Genesis.Simulation.Achievements;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.IO;

namespace Genesis;

public class Game1 : Game
{
    private const int MDefaultScreenWidth = 1280;
    private const int MDefaultScreenHeight = 720;

    private readonly Color mDefaultBackgroundColor = new Color(0x26, 0x28, 0x44);

    private readonly GraphicsDeviceManager mGraphicsManager;
    private SpriteBatch mSpriteBatch;
    private SpriteFont mDebugFont;
    private Texture2D mDebugTexture;
    private DebugOverlay mDebugOverlay;

    private bool mWasF10Pressed = false;

    private Arch.Core.World mWorld;

    private SpatialHash mSpatialHash;

    private SystemManager mSystemManager;
    private GameStateManager mGameStateManager;
    private InputService mInputService;
    private GameServices mGameServices;
    private ScreenService mScreenService;
    private AudioService mAudioService;
    private CameraService mCameraService;
    private FactoryService mFactoryService;

    public Game1()
    {
        mGraphicsManager = new GraphicsDeviceManager(this);

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        var metaData = SaveManager.LoadMeta();
        mScreenService = new ScreenService(mGraphicsManager, Window);
        mScreenService.SetResolution(MDefaultScreenWidth, MDefaultScreenHeight);

        mWorld = Arch.Core.World.Create();
        mWorld.SetResource(new MetaDataComponent(metaData));

        mSpatialHash = new SpatialHash(64);
        mInputService = new InputService();
        mAudioService = new AudioService(Content);
        mAudioService.Settings = metaData.AudioSettings;
        mCameraService = new CameraService();

        // Initialize Factory Service
        var effectFactory = new EffectFactory(Content);
        var projectileFactory = new ProjectileEntity(Content, mAudioService);
        var explosivesFactory = new ExplosivesFactory(Content, mAudioService);
        var enemyFactory = new EnemyFactory(Content);
        var minigunFactory = new MinigunFactory(Content);
        mFactoryService = new FactoryService(projectileFactory, explosivesFactory, effectFactory, enemyFactory, minigunFactory);

        // Note: DrawSystem registered later in LoadContent, since it is asset-dependent.
        mSystemManager = new SystemManager();

        // Create UI factory service (non-static)
        var uiFactory = new UiFactoryService(mAudioService);

        // Create item asset service and load assets.
        var itemAssets = new ItemAssetService(Content);
        itemAssets.LoadAssets();

        // Create a state manager and inject the world & systems + uiFactory
        mGameServices = new GameServices(
            mWorld,
            mSystemManager,
            Content,
            uiFactory,
            itemAssets,
            mInputService,
            metaData
        );

        // Initialize the factory now that GameServices exists
        uiFactory.Initialize(mGameServices, mAudioService);

        mGameStateManager = new GameStateManager(this, mGameServices, mScreenService, mCameraService, mAudioService);

        RegisterSystems(mGameStateManager, mScreenService, mCameraService, mFactoryService);
        // Start the game by setting a state
        mGameStateManager.ChangeState(new MainMenuState());

        mDebugOverlay = new DebugOverlay(this, mWorld, mAudioService);
        base.Initialize();

        // removed debug startup test file
    }

    private void RegisterSystems(GameStateManager gameState, ScreenService screen, CameraService camera, FactoryService factories)
    {
        var achievementUnlocker = new AchievementUnlocker(mGameServices);
        var rng = new RandomService(Environment.TickCount);

        mSystemManager.Add(new ButtonInputSystem(screen), SystemGroup.Core);
        mSystemManager.Add(new UiSliderSystem(screen), SystemGroup.Core);
        mSystemManager.Add(new PlayerInputSystem(), SystemGroup.Core);
        mSystemManager.Add(new SaveSystem(), SystemGroup.Core);
        mSystemManager.Add(new InventorySystem(), SystemGroup.Core);
        mSystemManager.Add(new InteractionSystem(gameState, mAudioService), SystemGroup.Core);
        mSystemManager.Add(new ConsumptionSystem(Content,factories, camera, screen, mAudioService), SystemGroup.Core);
        mSystemManager.Add(new HotbarInputSystem(), SystemGroup.Core);

        mSystemManager.Add(new EnemyControlSystem(factories, Content, mAudioService, mSpatialHash, rng), SystemGroup.Gameplay);
        mSystemManager.Add(new CompanionControlSystem(factories, mAudioService, mSpatialHash), SystemGroup.Gameplay);
        mSystemManager.Add(new LifeTimeSystem(), SystemGroup.Gameplay);
        mSystemManager.Add(new PlayerFacingSystem(camera), SystemGroup.Gameplay);
        mSystemManager.Add(new StaminaSystem(), SystemGroup.Gameplay);
        mSystemManager.Add(new AttackSystem(factories, mAudioService, mSpatialHash), SystemGroup.Gameplay);
        mSystemManager.Add(new AcidHazardSystem(mSpatialHash), SystemGroup.Gameplay);
        mSystemManager.Add(new PurchaseSystem(mAudioService), SystemGroup.Gameplay);
        mSystemManager.Add(new TrapSystem(mAudioService), SystemGroup.Gameplay);
        mSystemManager.Add(new DoorSystem(), SystemGroup.Gameplay);
        mSystemManager.Add(new RoomTransitionSystem(gameState, GraphicsDevice), SystemGroup.Gameplay);
        mSystemManager.Add(new LayerTransitionSystem(gameState,GraphicsDevice), SystemGroup.Gameplay);
        mSystemManager.Add(new DeathSystem(gameState), SystemGroup.Gameplay);
        mSystemManager.Add(new CooldownSystem(), SystemGroup.Gameplay);
        mSystemManager.Add(new BloodlustSystem(achievementUnlocker), SystemGroup.Gameplay);
        mSystemManager.Add(new MinigunControlSystem(factories, mAudioService), SystemGroup.Gameplay);
        mSystemManager.Add(new EquipmentSystem(),  SystemGroup.Gameplay);
        mSystemManager.Add(new StatuseffectSystem(),  SystemGroup.Gameplay);
        mSystemManager.Add(new ChemicalTankSystem(Content, mAudioService), SystemGroup.Gameplay);
        mSystemManager.Add(new TableSystem(Content, mAudioService), SystemGroup.Gameplay);
        mSystemManager.Add(new RunTimerSystem(), SystemGroup.Gameplay);
        mSystemManager.Add(new TutorialTriggerSystem(gameState), SystemGroup.Gameplay);

        mSystemManager.Add(new EntityCollisionSystem(mSpatialHash), SystemGroup.Physics);
        mSystemManager.Add(new MovementSystem(mSpatialHash), SystemGroup.Physics);

        mSystemManager.Add(new AnimationSystem(), SystemGroup.Presentation);
        mSystemManager.Add(new MovementSoundSystem(mAudioService), SystemGroup.Presentation);
        mSystemManager.Add(new ProximityLightSystem(), SystemGroup.Presentation);
        mSystemManager.Add(new VisualStateSystem(), SystemGroup.Presentation);
        mSystemManager.Add(new CullingSystem(mCameraService), SystemGroup.Presentation);
        mSystemManager.Add(new ScreenShakingSystem(camera, rng), SystemGroup.Presentation);

        mSystemManager.Add(new TiledMapRenderSystem(camera), SystemGroup.Rendering);

        mSystemManager.Add(new CompanionHeartSystem(), SystemGroup.Presentation);
    }

    protected override void LoadContent()
    {
        mSpriteBatch = new SpriteBatch(GraphicsDevice);
        mDebugOverlay.RebuildFontAtlas();

        mDebugFont = Content.Load<SpriteFont>("Fonts/DebugFont");
        mDebugTexture = new Texture2D(GraphicsDevice, 1, 1);
        mDebugTexture.SetData(new[] { Color.White });

        // TODO for debug! move DamageSystem registration to Initialize later!
        var drawSystem = new DrawSystem(mScreenService, mDebugFont, mDebugOverlay);
        mSystemManager.Add(drawSystem, SystemGroup.Rendering);
        mSystemManager.Add(new DamageSystem(drawSystem), SystemGroup.Physics);
        mSystemManager.Add(new DebugRenderSystem(mDebugOverlay, mDebugTexture, mDebugFont), SystemGroup.Rendering);
    }

    protected override void Update(GameTime gameTime)
    {
        // 1. Update the hardware status
        mInputService.Update(IsActive, gameTime);

        // 2. The active state reacts to input
        mGameStateManager.HandleInput(mInputService);

        // 3. Simulate the game world
        mGameStateManager.Update(gameTime);

        // 4. Update the audio service
        mAudioService.Update(gameTime);

        // 5. Update the MonoGame framework
        base.Update(gameTime);

        // 6. Update the debug overlay
        mDebugOverlay.Update();
    }

    protected override void Draw(GameTime gameTime)
    {
        // Set background color based on state
        Color clearColor = mDefaultBackgroundColor;

        // Black for title screen
        if (mGameStateManager.IsTopState<MainMenuState>())
        {
            clearColor = Color.Black;
        }

        mScreenService.Graphics.Clear(clearColor);

        // Drawing is delegated to the GameState
        mGameStateManager.Draw(gameTime, mSpriteBatch);

        // Re-render the frame into a temporary RenderTarget2D when F10 is
        // pressed so we capture everything (RT-based content, ImGui, HUD).
        KeyboardState kb = Keyboard.GetState();
        bool isF10 = kb.IsKeyDown(Keys.F10);

        if (isF10 && !mWasF10Pressed)
        {
            int width = GraphicsDevice.PresentationParameters.BackBufferWidth;
            int height = GraphicsDevice.PresentationParameters.BackBufferHeight;

            RenderTarget2D rt = new RenderTarget2D(GraphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.None);
            // Remember previous targets
            var prev = GraphicsDevice.GetRenderTargets();

            GraphicsDevice.SetRenderTarget(rt);
            GraphicsDevice.Clear(clearColor);

            // Re-run the normal draws into the RT
            mGameStateManager.Draw(gameTime, mSpriteBatch);
            mDebugOverlay.Draw(gameTime);

            // Restore previous target(s)
            GraphicsDevice.SetRenderTargets(prev);

            // Save RT to disk
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string screenshotsDirectory = Path.Combine(baseDir, "Screenshots");
                if (!Directory.Exists(screenshotsDirectory)) Directory.CreateDirectory(screenshotsDirectory);
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string fileName = Path.Combine(screenshotsDirectory, $"screenshot_{timestamp}.png");

                using (var stream = File.Create(fileName))
                {
                    rt.SaveAsPng(stream, width, height);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save screenshot: {ex}");
            }

            rt.Dispose();
        }

        mWasF10Pressed = isF10;

        base.Draw(gameTime);

        mDebugOverlay.Draw(gameTime);
    }
}