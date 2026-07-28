using System;
using System.Collections.Generic;
using Arch.Core;
using Genesis.Architecture;
using Genesis.Architecture.Audio;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Systems;
using Genesis.GameStates.Menus;
using Genesis.GameStates.Menus.Settings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Genesis.GameStates.Overlays;

/// <summary>
/// ConfirmState , where you can Proceed with the onClick action or return  
/// </summary>
/// <param name="ConfirmAction">Onclick Action to Procced if Proccedbutton clicked</param>
public class ConfirmState (Action confirmAction) : IGameState 
{
    private World mUiWorld;
    private GameStateManager mStateManager;
    private GameServices mServices;
    private ScreenService mScreenService;
    private AudioService mSound;
    private Action mConfirmAction = confirmAction;

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
        if (input.IsActionPressed(InputAction.Pause)) { mStateManager.PopState(); return; }
        mServices.Systems.Get<ButtonInputSystem>().HandleInput(mUiWorld, input);
    }

    public void Update(GameTime gameTime) { }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        // drawbelowtop geht nicht wegen rekursionsfehler

        // Draws the pause menu on top
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

        const int buttonCount = 2;
        
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
        mServices.UiFactory.CreateText(mUiWorld, new Vector2(positionX, positionY - gap),"Unsaved progress will be lost.", UiFactoryService.mStandartFont, Color.IndianRed,
            TextAlignment.MiddleCenter);

    mButtons.Add(mServices.UiFactory.CreateButtonWithSprite(
            world: mUiWorld,
            position: new Vector2(positionX, positionY),
            text: "Proceed",
            onClick: () => mConfirmAction(),
            targetPixels: targetPixels,
            padding: padding));

        mButtons.Add(mServices.UiFactory.CreateButtonWithSprite(
            world: mUiWorld,
            position: new Vector2(positionX, positionY + gap),
            text: "Return",
            onClick: () => mStateManager.PopState(),
            targetPixels: targetPixels,
            padding: padding));
    }
}