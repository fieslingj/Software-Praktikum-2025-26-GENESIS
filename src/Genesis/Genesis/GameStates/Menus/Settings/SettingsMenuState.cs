using System.Collections.Generic;
using Arch.Core;
using Genesis.Architecture;
using Genesis.Architecture.Audio;
using Genesis.Gameplay.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Genesis.GameStates.Menus.Settings;

public class SettingsMenuState : IGameState
{
    private World mUiWorld;

    private GameStateManager mStateManager;
    private GameServices mServices;
    private ScreenService mScreenService;

    private readonly List<Entity> mButtons = new();

    public void Initialize(GameStateManager manager, GameServices services, ScreenService screen, AudioService sound)
    {
        mStateManager = manager;
        mServices = services;
        mScreenService = screen;
    }

    public void Enter()
    {
        mUiWorld = World.Create();
        BuildUi();
    }

    public void Exit()
    {
        mUiWorld.Dispose();
    }
    public void Pause() { }
    public void Resume() { }

    public void HandleInput(InputService input)
    {
        // Meta actions on input
        if (input.IsActionPressed(InputAction.Pause)) { mStateManager.PopState(); return; }

        // Input systems
        mServices.Systems.Get<ButtonInputSystem>().HandleInput(mUiWorld, input);
    }

    public void Update(GameTime gameTime) { }

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

    private void BuildUi()
    {
        mButtons.Clear();

        const int buttonCount = 3;
        
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

        mButtons.Add(mServices.UiFactory.CreateButtonWithSprite(world: mUiWorld,
            position: new Vector2(positionX, positionY),
            text: "Display Settings",
            onClick: () => mStateManager.PushState(new DisplaySettingsMenuState()),
            targetPixels: targetPixels,
            padding: padding));

        mButtons.Add(mServices.UiFactory.CreateButtonWithSprite(world: mUiWorld,
            position: new Vector2(positionX, (positionY + gap)),
            text: "Sound Settings",
            onClick: () => mStateManager.PushState(new AudioSettingsState()),
            targetPixels: targetPixels,
            padding: padding));
        
        mButtons.Add(mServices.UiFactory.CreateButtonWithSprite(world: mUiWorld,
            position: new Vector2(positionX, (positionY + gap * 2)),
            text: "Keybindings",
            onClick: () => mStateManager.PushState(new KeyBindingsState()),
            targetPixels: targetPixels,
            padding: padding));

        mButtons.Add(mServices.UiFactory.CreateButtonWithSprite(world: mUiWorld,
            position: new Vector2(positionX, (positionY + (gap * 3))),
            text: "Return",
            onClick: () => mStateManager.PopState(),
            targetPixels: targetPixels,
            padding: padding));
    }
}