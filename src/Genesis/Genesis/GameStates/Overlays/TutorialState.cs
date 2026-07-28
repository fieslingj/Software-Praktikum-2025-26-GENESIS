using Arch.Core;
using Genesis.Architecture;
using Genesis.Architecture.Audio;
using Genesis.Gameplay.Components.World;
using Genesis.Gameplay.Systems;
using Genesis.GameStates.Overlays.Tutorial;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Genesis.GameStates.Overlays;

public class TutorialState : IGameState
{
    private GameStateManager mGameStateManager;
    private GameServices mServices;
    private ScreenService mScreenService;
    private AudioService mAudio;
    
    private TutorialRenderer mRenderer;
    private TutorialTypewriterEffect mTypewriter;
    private TutorialScrollHintAnimation mScrollHint;
    private TutorialTextPaginator mPaginator;
    
    private readonly string mTutorialText;
    private Texture2D mPixelTexture;
    
    private World mUiWorld;
    private Entity mContinueButton;
    private Entity mSkipButton;

    private static readonly QueryDescription sRunSessionQuery = new QueryDescription()
        .WithAll<RunSessionComponent>();


    // Constructor with optional custom tutorial text
    public TutorialState(string tutorialText)
    {
        mTutorialText = tutorialText ?? TutorialContent.MovementText;
    }

    public void Initialize(GameStateManager manager, GameServices services, ScreenService screen, AudioService sound)
    {
        mGameStateManager = manager;
        mServices = services;
        mScreenService = screen;
        mAudio = sound;
        
        var panelTexture = services.Content.Load<Texture2D>("Sprites/UI/TutorialRectangle");
        var font = services.Content.Load<SpriteFont>("Fonts/GenesisFont");
        
        mPixelTexture = new Texture2D(screen.Graphics, 1, 1);
        mPixelTexture.SetData([Color.White]);

        // Renders the tutorial overlay
        mRenderer = new TutorialRenderer(panelTexture, mPixelTexture, font);

        // Calculates layout based on panel texture
        var layoutCalculator = new TutorialLayoutCalculator(panelTexture);

        // Calculates max line width for text pagination
        var maxLineWidth = layoutCalculator.CalculateMaxLineWidth();

        // Paginates the tutorial text
        mPaginator = new TutorialTextPaginator(font, (int)maxLineWidth, TutorialContent.PageBreakLength);

        // Build pages from the tutorial text
        mPaginator.BuildPages(mTutorialText, maxLineWidth);

        // Initialize typewriter effect
        mTypewriter = new TutorialTypewriterEffect();

        // Start with the first page
        mTypewriter.StartNewText(mPaginator.GetCurrentPageText());

        // Initialize scroll hint animation
        mScrollHint = new TutorialScrollHintAnimation();
    }

    public void Enter()
    {
        mUiWorld = World.Create();
        BuildUi();
    }
    
    public void Exit()
    {
        mUiWorld?.Dispose();
    }
    
    public void Pause() { }
    
    public void Resume() { }

    public void HandleInput(InputService input)
    {
        
        if (input.IsActionPressed(InputAction.ScrollDown) || input.GetMouseScroll() < 0)
        {
            if (mPaginator.TryAdvanceToNextPage())
            {
                mTypewriter.StartNewText(mPaginator.GetCurrentPageText());
                mScrollHint.Reset();
            }
        }
        
        if (input.IsActionPressed(InputAction.ScrollUp) || input.GetMouseScroll() > 0)
        {
            if (mPaginator.TryGoToPreviousPage())
            {
                mTypewriter.StartNewText(mPaginator.GetCurrentPageText());
                mScrollHint.Reset();
            }
        }
        
        mServices.Systems.Get<ButtonInputSystem>().HandleInput(mUiWorld, input);
    }

    public void Update(GameTime gameTime)
    {
        mTypewriter.Update(gameTime);
        mScrollHint.Update(gameTime);
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        // Draw underlying game state first
        mGameStateManager.DrawBelowTop(gameTime, spriteBatch);

        // Draw tutorial overlay
        var uiScale = mScreenService.GetUiScale();
        var virtualWidth = (float)ScreenService.VirtualWidth;
        var virtualHeight = (float)ScreenService.VirtualHeight;
        var screenCenterX = virtualWidth / 2f;
        var screenCenterY = virtualHeight / 2f;
        var visibleText = mTypewriter.VisibleText;
        var showScrollHint = mPaginator.HasNextPage() && mTypewriter.IsComplete;
        var scrollHintVisible = mScrollHint.IsVisible;
        
        spriteBatch.Begin(
            samplerState: SamplerState.PointClamp,
            transformMatrix: Matrix.CreateScale(uiScale, uiScale, 1.0f),
            sortMode: SpriteSortMode.FrontToBack);
        
        mRenderer.DrawOverlay(
            spriteBatch,
            screenCenterX,
            screenCenterY,
            visibleText,
            showScrollHint,
            scrollHintVisible);

        // Draw UI elements (buttons)
        mServices.Systems.Get<DrawSystem>().Draw(mUiWorld, spriteBatch);
        spriteBatch.End();
    }
    
    private void BuildUi()
    {
        var virtualWidth = (float)ScreenService.VirtualWidth;
        
        var layoutCalculator = new TutorialLayoutCalculator(
            mServices.Content.Load<Texture2D>("Sprites/UI/TutorialRectangle"));

        // Get button size and position
        var buttonSize = layoutCalculator.CalculateButtonSize();
        var buttonY = layoutCalculator.CalculateButtonY();
        
        var paddingX = virtualWidth / 80f;
        var paddingY = virtualWidth / 80f;
        var targetPixels = new Rectangle(0, 0, (int)buttonSize.X, (int)buttonSize.Y);
        var padding = new Point((int)paddingX, (int)paddingY);

        // Back to Game-Button (right)
        var backPositionX = virtualWidth / 2f + virtualWidth / 12f + 20;
        
        mContinueButton = mServices.UiFactory.CreateButtonWithSprite(
            world: mUiWorld,
            position: new Vector2(backPositionX, buttonY),
            text: "Back to Game",
            onClick: () => mGameStateManager.PopState(),
            targetPixels: targetPixels,
            padding: padding);

        // Skip tutorial-Button (left)
        var skipPositionX = virtualWidth / 2f - virtualWidth / 12f - 20;
        
        mSkipButton = mServices.UiFactory.CreateButtonWithSprite(
            world: mUiWorld,
            position: new Vector2(skipPositionX, buttonY),
            text: "Skip Tutorial",
            onClick: OnSkipTutorial,
            targetPixels: targetPixels,
            padding: padding);
    }

    private void OnSkipTutorial()
    {
        // Deactivate tutorial in RunSessionComponent
        Entity runSessionEntity = Entity.Null;
        mServices.World.Query(in sRunSessionQuery, (Entity entity) => { runSessionEntity = entity; });
        
        if (runSessionEntity != Entity.Null)
        {
            ref var runSession = ref mServices.World.Get<RunSessionComponent>(runSessionEntity);
            runSession.TutorialActive = false;
        }
        mGameStateManager.PopState();
    }
}