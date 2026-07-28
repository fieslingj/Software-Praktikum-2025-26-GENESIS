using Arch.Core;
using Genesis.Architecture;
using Genesis.Architecture.Audio;
using Genesis.Architecture.Persistence;
using Genesis.Gameplay.Components.World;
using Genesis.Gameplay.Extensions;
using Genesis.GameStates.Menus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Genesis.Persistence.Run;

namespace Genesis.GameStates.Gameplay;

public class GameOverState : IGameState
{
    private GameStateManager mGameStateManager;
    private ScreenService mScreenService;
    private World mGameWorld;
    private SpriteFont mFont;
    private AudioService mSound;

    // Determine how transparent the black rectangle should be.
    // In this case you can see 40% of the original game world.
    private const float MOverlayMaxOpacity = 0.6f;

    // Animation Timers
    private float mTimer;
    private const float FadeInDuration = 2.0f;
    private const float LingerDuration = 3.0f;

    // "YOU DIED" Color
    private readonly Color mTextColor = new(180, 20, 20);

    private GameServices mServices;

    public void Initialize(GameStateManager manager, GameServices services, ScreenService screen, AudioService sound)
    {
        mGameStateManager = manager;
        mScreenService = screen;
        mGameWorld = services.World;
        mServices = services;
        mFont = services.Content.Load<SpriteFont>("Fonts/TitleFont");
        mSound = sound;
    }

    public void Enter()
    {
        DeleteRun();
        mTimer = 0f;
        SavedStatisticData.Fetch(mGameWorld, StatisticCallingState.GameOver, mServices);
        mSound.PlayMusic("Sounds/Effects/gameover", isRepeating: false);
    }

    public void Exit() {}
    public void Pause() {}
    public void Resume() {}

    public void HandleInput(InputService input) {}

    public void Update(GameTime gameTime)
    {
        var deltaTime = (float) gameTime.ElapsedGameTime.TotalSeconds;
        mTimer += deltaTime;

        if (mTimer > FadeInDuration + LingerDuration)
        {mGameStateManager.ChangeState(new MainMenuState());}
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var alpha = MathHelper.Clamp(mTimer / FadeInDuration, 0.0f, 1.0f);

        spriteBatch.Begin(
            transformMatrix: mScreenService.Adapter.GetScaleMatrix(),
            samplerState: SamplerState.LinearClamp,
            blendState: BlendState.AlphaBlend
        );

        // Background fades in
        spriteBatch.Draw(
            GetPixel(spriteBatch.GraphicsDevice),
            new Rectangle(0, 0, mScreenService.Adapter.VirtualWidth, mScreenService.Adapter.VirtualHeight),
            Color.Black * MOverlayMaxOpacity * alpha
        );

        // Text plus Text-Shadow fades in
        const string text = "YOU DIED";
        var size = mFont.MeasureString(text);
        var center = new Vector2(mScreenService.Adapter.VirtualWidth / 2f, mScreenService.Adapter.VirtualHeight / 2f);
        var pos = center - (size / 2);

        spriteBatch.DrawString(mFont, text, pos + new Vector2(2,2), Color.Black * alpha);
        spriteBatch.DrawString(mFont, text, pos, mTextColor * alpha);
        spriteBatch.End();
    }

    private Texture2D mPixel;

    private Texture2D GetPixel(GraphicsDevice graphicsDevice)
    {
        if (mPixel is not null) {return mPixel;}

        mPixel = new Texture2D(graphicsDevice, 1, 1);
        mPixel.SetData([Color.White]);
        return mPixel;
    }

    private bool DeleteRun()
    {
        var session = mGameWorld.GetResource<RunSessionComponent>();
        switch (session.SlotIndex)
        {
            case null:
            case < 0 or > SaveManager.MaxSaveSlots:
                return false;
            
            case { } slot:
                SaveManager.DeleteRun(slot);
                break;
        }

        return true;
    }
}