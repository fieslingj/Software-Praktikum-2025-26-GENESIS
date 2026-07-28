using System;
using System.Collections.Generic;
using Arch.Core;
using Genesis.Architecture;
using Genesis.Architecture.Audio;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Components.Visuals;
using Genesis.Gameplay.Systems;
using Genesis.GameStates.Core;
using Genesis.GameStates.Menus.Settings;
using Genesis.Simulation.LoadingTasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Genesis.GameStates.Menus;

public class MainMenuState : IGameState
{
    private World mUiWorld;
    private GameStateManager mStateManager;
    private GameServices mServices;
    private ScreenService mScreenService;
    private AudioService mSound;

    private readonly List<Entity> mButtons = new();

    public void Initialize(GameStateManager manager, GameServices services, ScreenService screen, AudioService sound)
    {
        mStateManager = manager;
        mServices = services;
        mScreenService = screen;
        mSound = sound;
    }

    public void Enter()
    {
        mUiWorld = World.Create();

        CreateBackground();
        
        mSound.PlayMusic("Sounds/Music/Title");

        BuildUi();
    }

    public void Exit()
    {
        mUiWorld.Dispose();
    }

    public void Pause() { }

    public void Resume()
    {
    }

    public void HandleInput(InputService input)
    {
        // 1. meta actions on input
        if (input.IsActionPressed(InputAction.Pause)) { mStateManager.ExitGame(); return; }

        // 2. input systems
        mServices.Systems.Get<ButtonInputSystem>().HandleInput(mUiWorld, input);
    }

    public void Update(GameTime gameTime)
    {
        mServices.Systems.Get<AnimationSystem>().Update(mUiWorld, gameTime);
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var uiScale = mScreenService.GetUiScale();
        spriteBatch.Begin(
            samplerState: SamplerState.PointClamp,
            transformMatrix: Matrix.CreateScale(uiScale, uiScale, 1.0f),
            sortMode: SpriteSortMode.FrontToBack
        );
        mServices.Systems.Get<DrawSystem>().Draw(mUiWorld, spriteBatch);
        spriteBatch.End();
    }

    private void CreateBackground()
    {
        var texture = mServices.Content.Load<Texture2D>("Sprites/Backgrounds/TitleAnimation");

        int frameCount = 36;
        int framesPerRow = 6;
        int frameWidth = 688;
        int frameHeight = 340;
        float frameDuration = 100f;

        float scaleX = (float)ScreenService.VirtualWidth / frameWidth;
        float scaleY = (float)ScreenService.VirtualHeight / frameHeight;
        float finalScale = Math.Max(scaleX, scaleY);

        var centerPos = new Vector2(ScreenService.VirtualWidth / 2f, ScreenService.VirtualHeight / 2f);

        var bgEntity = mServices.UiFactory.MarkAsStaticUi(mUiWorld, mUiWorld.Create());

        mUiWorld.Add(bgEntity,
            new PositionComponent(centerPos),
            new SpriteComponent(
                spriteSheet: texture,
                sourceRect: new Rectangle(0, 0, frameWidth, frameHeight),
                layerDepth: 0.0f,
                scale: finalScale
            )
            {
                Origin = new Vector2(frameWidth / 2f, frameHeight / 2f)
            },
            new SimpleAnimationComponent(
                frameWidth: frameWidth,
                frameHeight: frameHeight,
                frameCount: frameCount,
                framesPerRow: framesPerRow,
                frameDuration: frameDuration,
                isLooping: true
            )
        );
    }

    private void BuildUi()
    {
        mButtons.Clear();

        const int buttonCount = 7;

        var virtualWidth = (float)ScreenService.VirtualWidth;
        var virtualHeight = (float)ScreenService.VirtualHeight;

        // Set the button positions
        var gap = virtualHeight / 8f;

        var positionX = virtualWidth / 2f;
        var positionY = (virtualHeight - (buttonCount - 1) * gap) / 2f;

        // Button size settings
        var buttonWidth = (virtualWidth / 6f);
        var buttonHeight = (virtualWidth / 30f);
        var paddingX = (virtualWidth / 80f);
        var paddingY = (virtualWidth / 80f);

        var targetPixels = new Rectangle(0, 0, (int)buttonWidth, (int)buttonHeight);
        var padding = new Point((int)paddingX, (int)paddingY);

        mButtons.Add(mServices.UiFactory.CreateButtonWithSprite(
            world: mUiWorld,
            position: new Vector2(positionX, positionY + gap * 0),
            text: "New Game",
            onClick: () => mStateManager.PushState(new ChooseCharacterState()),
            targetPixels: targetPixels,
            padding: padding));


        mButtons.Add(mServices.UiFactory.CreateButtonWithSprite(
            world: mUiWorld,
            position: new Vector2(positionX, positionY + gap * 1),
            text: "Load Game",
            onClick: () => mStateManager.PushState(new LoadMenuState()),
            targetPixels: targetPixels,
            padding: padding));


        mButtons.Add(mServices.UiFactory.CreateButtonWithSprite(
            world: mUiWorld,
            position: new Vector2(positionX, positionY + gap * 2),
            text: "Statistics",
            onClick: () => mStateManager.PushState(new StatisticsState()),
            targetPixels: targetPixels,
            padding: padding));


        mButtons.Add(mServices.UiFactory.CreateButtonWithSprite(
            world: mUiWorld,
            position: new Vector2(positionX, positionY + gap * 3),
            text: "Achievements",
            onClick: () => mStateManager.PushState(new AchivementsState()),
            targetPixels: targetPixels,
            padding: padding));


        mButtons.Add(mServices.UiFactory.CreateButtonWithSprite(
            world: mUiWorld,
            position: new Vector2(positionX, positionY + gap * 4),
            text: "Techdemo",
            onClick: () => mStateManager.PushState(new LoadingState(new LoadTechdemoTask(), mScreenService.Graphics, true)),
            targetPixels: targetPixels,
            padding: padding));


        mButtons.Add(mServices.UiFactory.CreateButtonWithSprite(
            world: mUiWorld,
            position: new Vector2(positionX, positionY + gap * 5),
            text: "Settings",
            onClick: () => mStateManager.PushState(new SettingsMenuState()),
            targetPixels: targetPixels,
            padding: padding));


        mButtons.Add(mServices.UiFactory.CreateButtonWithSprite(
            world: mUiWorld,
            position: new Vector2(positionX, positionY + gap * 6),
            text: "Quit",
            onClick: () => mStateManager.ExitGame(),
            targetPixels: targetPixels,
            padding: padding));
    }
}