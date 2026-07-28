using System.Collections.Generic;
using Arch.Core;
using Genesis.Architecture;
using Genesis.Architecture.Audio;
using Genesis.Architecture.Persistence;
using Genesis.Gameplay.Systems;
using Genesis.GameStates.Core;
using Genesis.Simulation.LoadingTasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
// Leave it here for the date formatting in `GetSlotText` method. If you want to swap to the other format, make sure to also add `using System.Globalization;` at the top of this file.
// using System.Globalization;

namespace Genesis.GameStates.Menus;

public class LoadMenuState : IGameState
{
    private World mUiWorld;

    private GameStateManager mStateManager;
    private GameServices mServices;
    private ScreenService mScreenService;
    private AudioService mAudioService;

    private readonly List<Entity> mButtons = new();

    public void Initialize(GameStateManager manager, GameServices services, ScreenService screen, AudioService sound)
    {
        mStateManager = manager;
        mServices = services;
        mScreenService = screen;
        mAudioService = sound;
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
        var buttonWidth = (virtualWidth / 4f);
        var buttonHeight = (virtualWidth / 30f);
        var paddingX = (virtualWidth / 80f);
        var paddingY = (virtualWidth / 80f);

        var targetPixels = new Rectangle(0, 0, (int)buttonWidth, (int)buttonHeight);
        var padding = new Point((int)paddingX, (int)paddingY);

        mButtons.Add(mServices.UiFactory.CreateButtonWithSprite(
            world: mUiWorld,
            position: new Vector2(positionX, positionY + (gap * 0)),
            text: GetSlotText(0),
            onClick: () => {
                if (SaveManager.LoadRun(0) != null)
                {
                    mStateManager.ChangeState(new LoadingState(new LoadFromSaveTask(0), mScreenService.Graphics));
                }
                else
                {
                    // play error sound and suppress the default button confirm sound
                    mAudioService.SuppressNextConfirmSound = true;
                    mAudioService.PlaySfx("Sounds/UI/ErrorSound");
                }},
            targetPixels: targetPixels,
            padding: padding));

        mButtons.Add(mServices.UiFactory.CreateButtonWithSprite(
            world: mUiWorld,
            position: new Vector2(positionX, positionY + (gap * 1)),
            text: GetSlotText(1),
            onClick: () => {
                if (SaveManager.LoadRun(1) != null)
                {
                    mStateManager.ChangeState(new LoadingState(new LoadFromSaveTask(1), mScreenService.Graphics));
                }
                else
                {
                    // play error sound and suppress the default button confirm sound
                    mAudioService.SuppressNextConfirmSound = true;
                    mAudioService.PlaySfx("Sounds/UI/ErrorSound");
                }},
            targetPixels: targetPixels,
            padding: padding));

        mButtons.Add(mServices.UiFactory.CreateButtonWithSprite(
            world: mUiWorld,
            position: new Vector2(positionX, positionY + (gap * 2)),
            text: GetSlotText(2),
            onClick: () => {
                if (SaveManager.LoadRun(2) != null)
                {
                    mStateManager.ChangeState(new LoadingState(new LoadFromSaveTask(2), mScreenService.Graphics));
                }
                else
                {
                    // play error sound and suppress the default button confirm sound
                    mAudioService.SuppressNextConfirmSound = true;
                    mAudioService.PlaySfx("Sounds/UI/ErrorSound");
                }},
            targetPixels: targetPixels,
            padding: padding));

        mButtons.Add(mServices.UiFactory.CreateButtonWithSprite(
            world: mUiWorld,
            position: new Vector2(positionX, positionY + (gap * 3)),
            text: "Return",
            onClick: () => mStateManager.PopState(),
            targetPixels: targetPixels,
            padding: padding));
    }

    private string GetSlotText(int slotIndex)
    {
        var data = SaveManager.LoadRun(slotIndex);
        if (data == null) return $"Slot {slotIndex + 1} - Empty";

        // Formats date and time from the save file
        // If you want to swap to the other format down below, make sure to also add `using System.Globalization;` at the top of this file.
        // Example output: "Slot 1 - 06.02.25 14:33"
        return $"Slot {slotIndex + 1} - {data.Date.ToLocalTime():dd.MM.yy HH:mm}";

        // Example output with culture-specific formatting: "Slot 1 - 02/06/25 02:33 PM"
        // return $"Slot {slotIndex + 1} - {data.Date.ToLocalTime().ToString("MM/dd/yy hh:mm tt", CultureInfo.InvariantCulture)}";
    }
}