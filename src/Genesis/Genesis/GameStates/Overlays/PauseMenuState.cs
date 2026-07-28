using System.Collections.Generic;
using Arch.Core;
using Genesis.Architecture;
using Genesis.Architecture.Audio;
using Genesis.Architecture.Persistence;
using Genesis.Gameplay.Components.World;
using Genesis.Gameplay.Extensions;
using Genesis.Gameplay.Systems;
using Genesis.GameStates.Menus;
using Genesis.GameStates.Menus.Settings;
using Genesis.Persistence.Run;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Genesis.GameStates.Overlays;

public class PauseMenuState : IGameState
{
    private World mUiWorld;
    private GameStateManager mStateManager;
    private GameServices mServices;
    private ScreenService mScreenService;
    private AudioService mSound;
    private World mWorld;

    private readonly List<Entity> mButtons = new();

    public void Initialize(GameStateManager manager, GameServices services, ScreenService screen, AudioService sound)
    {
        mStateManager = manager;
        mServices = services;
        mScreenService = screen;
        mSound = sound;
        mWorld = services.World;
    }

    public void Enter()
    {
        CaptureFloorState();
        mUiWorld = World.Create();
        SavedStatisticData.Fetch(mWorld, StatisticCallingState.Generic, mServices);
        mSound.PlayMusic("Sounds/Music/Theme");
        BuildUi();
    }

    public void Exit()
    {
        mSound.PauseMusic();
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
        // Draws the game in paused state
        mStateManager.DrawBelowTop(gameTime, spriteBatch);

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

        const int buttonCount = 6;
        
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
            position: new Vector2(positionX, positionY),
            text: "Continue",
            onClick: () => mStateManager.PopState(),
            targetPixels: targetPixels,
            padding: padding));

        mButtons.Add(mServices.UiFactory.CreateButtonWithSprite(
            world: mUiWorld,
            position: new Vector2(positionX, positionY + gap * 1),
            text: "Save",
            onClick: TrySaveRun,
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
            text: "Settings",
            onClick: () => mStateManager.PushState(new SettingsMenuState()),
            targetPixels: targetPixels,
            padding: padding));

        mButtons.Add(mServices.UiFactory.CreateButtonWithSprite(
            world: mUiWorld,
            position: new Vector2(positionX, positionY + gap * 5),
            text: "Main Menu",
            onClick: () => mStateManager.PushState(new ConfirmState( () => mStateManager.ChangeState(new MainMenuState()))),
            targetPixels: targetPixels,
            padding: padding));
    }

    private void TrySaveRun()
    {
        if (mWorld.TryGetResource<RunSessionComponent>(out var runSession) && runSession.SlotIndex is {} slot)
        {
            SaveManager.SaveRun(mWorld, slot);
        }
        else
        {
            mStateManager.PushState(new SaveMenuState());
        }
    }

    private void CaptureFloorState()
    {
        if (!mWorld.TryGetResource<FloorLayoutComponent>(out var floorLayout)) return;

        floorLayout.CurrentRoom.Enemies = mWorld.FetchAllEnemies();
        floorLayout.CurrentRoom.Traps = mWorld.FetchAllTraps();
        floorLayout.CurrentRoom.Corpses = mWorld.FetchAllCorpses();
        floorLayout.CurrentRoom.RemoteExplosives = mWorld.FetchAllExplosives();
        mWorld.SetResource(floorLayout);
    }
}