using System;
using Arch.Core;
using Genesis.Architecture;
using Genesis.Architecture.Audio;
using Genesis.Architecture.Persistence;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Components.UI;
using Genesis.Gameplay.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Genesis.GameStates.Menus.Settings;

public class AudioSettingsState() : IGameState
{
    private readonly World mUiWorld = World.Create();
    
    private GameStateManager mGameStateManager;
    private GameServices mServices;
    private ScreenService mScreenService;

    private AudioService mAudioService;
    
    public void Initialize(GameStateManager manager, GameServices services, ScreenService screen, AudioService sound)
    {
        mGameStateManager = manager;
        mServices = services;
        mScreenService = screen;
        mAudioService = sound;
    }

    public void Enter() => BuildUi(mUiWorld);
    public void Exit()
    {
        mUiWorld.Dispose();
        if (!mServices.World.TryGetResource<MetaDataComponent>(out var metaData)) { return; }
        metaData.Data.AudioSettings = mAudioService.Settings;
        mServices.World.SetResource(metaData);
        SaveManager.SaveMeta(metaData.Data);
    }

    public void Pause() {}
    public void Resume() {}

    public void HandleInput(InputService input)
    {
        if (input.IsActionPressed(InputAction.Pause))
        {
            mGameStateManager.PopState();
            return;
        }
        
        mServices.Systems.HandleInput(mUiWorld, input);
    }

    public void Update(GameTime gameTime) => mServices.Systems.Update(mUiWorld, gameTime);

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var uiScale = mScreenService.GetUiScale();
        spriteBatch.Begin(
            samplerState: SamplerState.PointClamp,
            transformMatrix: Matrix.CreateScale(uiScale, uiScale, 1.0f),
            sortMode: SpriteSortMode.FrontToBack
        );
        mServices.Systems.Draw(mUiWorld, spriteBatch);
        spriteBatch.End();
    }

    private void BuildUi(World world)
    {
        const int nodeCount = 7;
        const float virtualWidth = ScreenService.VirtualWidth;
        const float virtualHeight = ScreenService.VirtualHeight;
        const float nodeGap = virtualHeight / 8f;
        
        const float startPositionX = virtualWidth / 2f;
        const float startPositionY = (virtualHeight - (nodeCount - 1) * nodeGap) / 2f;
        const int nodeWidth = (int)virtualWidth / 6;
        const int nodeHeight = (int)virtualHeight / 20;
        const int paddingX = (int)virtualWidth / 80;
        const int paddingY = (int)virtualHeight / 80;
        
        var targetPixels = new Rectangle(0, 0, nodeWidth, nodeHeight);
        var padding = new Point(paddingX, paddingY);
        
        var textFont = mServices.Content.Load<SpriteFont>("Fonts/HudFont");

        mServices.UiFactory.CreateText(
            world,
            new Vector2(startPositionX, startPositionY + 0 * nodeGap),
            "Master Volume",
            textFont,
            Color.White,
            TextAlignment.MiddleCenter
        );
        CreateSlider(
            world,
            new Rectangle((int)startPositionX, (int)(startPositionY + 1 * nodeGap), nodeWidth, nodeHeight),
            mAudioService.Settings.MasterVolume,
            f => ApplyChange(value => mAudioService.Settings.MasterVolume = value, f)
        );
        
        mServices.UiFactory.CreateText(
            world,
            new Vector2(startPositionX, startPositionY + 2 * nodeGap),
            "Music Volume",
            textFont,
            Color.White,
            TextAlignment.MiddleCenter
        );
        CreateSlider(
            world,
            new Rectangle((int)startPositionX, (int)(startPositionY + 3 * nodeGap), nodeWidth, nodeHeight),
            mAudioService.Settings.MusicVolume,
            f => ApplyChange(value => mAudioService.Settings.MusicVolume = value, f)
        );
        
        mServices.UiFactory.CreateText(
            world,
            new Vector2(startPositionX, startPositionY + 4 * nodeGap),
            "Sound Effect Volume",
            textFont,
            Color.White,
            TextAlignment.MiddleCenter
        );
        CreateSlider(
            world,
            new Rectangle((int)startPositionX, (int)(startPositionY + 5 * nodeGap), nodeWidth, nodeHeight),
            mAudioService.Settings.SfxVolume,
            f => ApplyChange(value => mAudioService.Settings.SfxVolume = value, f)
        );

        mServices.UiFactory.CreateButtonWithSprite(
            world: world,
            position: new Vector2(startPositionX, startPositionY + 6 * nodeGap),
            text: "Return",
            onClick: () => mGameStateManager.PopState(),
            targetPixels: targetPixels,
            padding: padding
        );
    }

    private void CreateSlider(World world, Rectangle bounds, float initial, Action<float> onSet)
    {
        const float minValue = 0f;
        const float maxValue = 1f;

        world.Create(
            new PositionComponent(new Vector2(bounds.X, bounds.Y)),
            new UiSliderComponent(bounds, minValue, maxValue, initial, onSet)
        );
    }

    private void ApplyChange(Action<float> change, float value)
    {
        change.Invoke(value);
        mAudioService.UpdateMusicVolume();
    }
}