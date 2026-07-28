using System.Collections.Generic;
using Arch.Core;
using Genesis.Architecture;
using Genesis.Architecture.Audio;
using Genesis.Architecture.Persistence;
using Genesis.Gameplay.Components.World;
using Genesis.Gameplay.Extensions;
using Genesis.Gameplay.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Genesis.GameStates.Menus;

public class SaveMenuState : IGameState
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
        // 1. meta actions on input
        if (input.IsActionPressed(InputAction.Pause)) { mStateManager.PopState(); return; }

        // 2. input systems
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

        const int buttonCount = 4;
        
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
            text: "Slot 1",
            onClick: () => OnSaveButtonClicked(0),
            targetPixels: targetPixels,
            padding: padding));

        mButtons.Add(mServices.UiFactory.CreateButtonWithSprite(
            world: mUiWorld,
            position: new Vector2(positionX, positionY + gap * 1),
            text: "Slot 2",
            onClick: () => OnSaveButtonClicked(1),
            targetPixels: targetPixels,
            padding: padding));

        mButtons.Add(mServices.UiFactory.CreateButtonWithSprite(
            world: mUiWorld,
            position: new Vector2(positionX, positionY + gap * 2),
            text: "Slot 3",
            onClick: () => OnSaveButtonClicked(2),
            targetPixels: targetPixels,
            padding: padding));

        mButtons.Add(mServices.UiFactory.CreateButtonWithSprite(
            world: mUiWorld,
            position: new Vector2(positionX, positionY + gap * 3),
            text: "Return",
            onClick: () => mStateManager.PopState(),
            targetPixels: targetPixels,
            padding: padding));
    }
    
    private void OnSaveButtonClicked(int selectedSlotIndex)
    {
        // Set the Run Session to the previously saved slot
        var world = mServices.World;
        var runSession = world.GetResource<RunSessionComponent>();
        runSession.SlotIndex = selectedSlotIndex;
        world.SetResource(runSession);
        
        // Save the run
        SaveManager.SaveRun(world, selectedSlotIndex);
        
        // Leave the Save Menu (provides visual feedback)
        mStateManager.PopState();
    }
}