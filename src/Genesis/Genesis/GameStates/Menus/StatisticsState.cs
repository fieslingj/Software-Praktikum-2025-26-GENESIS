using System;
using System.Collections.Generic;
using Arch.Core;
using Genesis.Architecture;
using Genesis.Architecture.Audio;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Generators;
using Genesis.Gameplay.Systems;
using Genesis.GameStates.Core;
using Genesis.Persistence.Meta;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Genesis.GameStates.Menus;

public class StatisticsState : IGameState
{
    private sealed class StatDefinition
    {
        public string Name { get; }
        public Func<string> ValueProvider { get; }

        public StatDefinition(string name, Func<string> valueProvider)
        {
            Name = name;
            ValueProvider = valueProvider;
        }
    }

    private GameStateManager mStateManager;
    private GameServices mServices;
    private ScreenService mScreenService;

    private World mUiWorld;

    private readonly List<Entity> mButtons = new();

    private readonly List<StatDefinition> mStats = new();

    private GlobalStatsData Stats => mServices.MetaData.Statistics;

    private string FormatTimeSeconds(float seconds)
    {
        if (seconds <= 0f) {return "-";}

        var ts = TimeSpan.FromSeconds(seconds);
        // Beispiel-Format: 01:23:45
        return ts.TotalHours >= 1
            ? $"{(int)ts.TotalHours:00}:{ts.Minutes:00}:{ts.Seconds:00}"
            : $"{ts.Minutes:00}:{ts.Seconds:00}";
    }

    private string FormatInt(int value) => value <= 0 ? "0" : value.ToString();
    private string FormatFloat(float value) => value <= 0f ? "0" : value.ToString("0.##");

    public void Initialize(GameStateManager manager, GameServices services, ScreenService screen, AudioService sound)
    {
        mStateManager = manager;
        mServices = services;
        mScreenService = screen;

        mStats.Clear();
        mStats.AddRange(
        [
            new StatDefinition(
                "Total Playtime",
                () => FormatTimeSeconds(Stats.TotalPlaytimeSeconds)
            ),
            new StatDefinition(
                "Fastest Run",
                () => FormatTimeSeconds(Stats.FastestRunSeconds)
            ),
            new StatDefinition(
                "Enemies Defeated",
                () => FormatInt(Stats.TotalEnemiesDefeated)
            ),
            new StatDefinition(
                "Total Damage Dealt",
                () => FormatFloat(Stats.TotalDamageDealt)
            ),
            new StatDefinition(
                "Rooms Explored",
                () => FormatInt(Stats.TotalRoomsExplored)
            ),
            new StatDefinition(
                "Successful Runs",
                () => FormatInt(Stats.TotalSuccessfulRuns)
            ),
            new StatDefinition(
                "Total Deaths",
                () => FormatInt(Stats.TotalDeaths)
            )
        ]);
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
        if (input.IsActionPressed(InputAction.Pause))
        {
            mStateManager.PopState();
            return;
        }

        // Run input systems
        mServices.Systems.HandleInput(mUiWorld, input);
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
        // load assets
        var textFont = mServices.Content.Load<SpriteFont>("Fonts/GenesisFont");
        var panelTexture = mServices.Content.Load<Texture2D>("Sprites/Buttons/StatisticBack");

        mButtons.Clear();
        mUiWorld ??= World.Create();

        var virtualWidth = (float)ScreenService.VirtualWidth;
        var virtualHeight = (float)ScreenService.VirtualHeight;

        // Place planel in the center of the screen
        var panelCenterX = virtualWidth * 0.5f;
        var panelCenterY = virtualHeight * 0.45f;

        // Set panel size and scale
        var panelTextureWidth = (float)panelTexture.Width;
        var panelTextureHeight = (float)panelTexture.Height;

        // Set target panel width to 70% of virtual width
        var targetPanelWidth = virtualWidth * 0.7f;
        var panelScale = targetPanelWidth / panelTextureWidth;

        var finalPanelWidth = panelTextureWidth * panelScale;
        var finalPanelHeight = panelTextureHeight * panelScale;

        // Set panel position
        var panelPosition = new Vector2(panelCenterX, panelCenterY);

        // Create panel entity
        var panelEntity = mServices.UiFactory.CreateImage(
            world: mUiWorld,
            position: panelPosition,
            texture: panelTexture,
            sourceRect: null,
            depth: 0.3f
        );

        float panelLeft;
        float panelRight;
        float panelTop;
        float panelBottom;

        if (mUiWorld.Has<SpriteComponent>(panelEntity))
        {
            ref var panelSprite = ref mUiWorld.Get<SpriteComponent>(panelEntity);
            panelSprite.mScale = panelScale;
            panelLeft = panelCenterX - finalPanelWidth * 0.5f;
            panelRight = panelCenterX + finalPanelWidth * 0.5f;
            panelTop = panelCenterY - finalPanelHeight * 0.5f;
            panelBottom = panelCenterY + finalPanelHeight * 0.5f;
        }
        else
        {
            // Fallback (without sprite component)
            panelLeft = panelCenterX - finalPanelWidth * 0.5f;
            panelRight = panelCenterX + finalPanelWidth * 0.5f;
            panelTop = panelCenterY - finalPanelHeight * 0.5f;
            panelBottom = panelCenterY + finalPanelHeight * 0.5f;
        }

        // Spacing inside the panel
        var innerMarginY = finalPanelHeight * 0.22f;
        var innerMarginX = finalPanelWidth * 0.15f;

        var innerTop = panelTop + innerMarginY;
        var innerBottom = panelBottom - innerMarginY;
        var innerLeft = panelLeft + innerMarginX;
        var innerRight = panelRight - innerMarginX;

        var usableHeight = innerBottom - innerTop;

        var rowCount = mStats.Count;
        var rowHeight = usableHeight / rowCount;

        // Column positions
        var nameX = innerLeft + innerMarginX * 0.3f;
        var valueX = innerRight - innerMarginX * 0.3f;

        var textColor = Color.Black;

        for (int i = 0; i < rowCount; i++)
        {
            var stat = mStats[i];
            var rowY = innerTop + rowHeight * (i + 0.5f);

            // Left row = Stat names
            mServices.UiFactory.CreateText(
                world: mUiWorld,
                position: new Vector2(nameX, rowY),
                text: stat.Name,
                font: textFont,
                color: textColor,
                alignment: TextAlignment.MiddleLeft
            );

            // Right row = Stat values
            mServices.UiFactory.CreateText(
                world: mUiWorld,
                position: new Vector2(valueX, rowY),
                text: stat.ValueProvider(),
                font: textFont,
                color: textColor,
                alignment: TextAlignment.MiddleRight
            );
        }

        // Return button
        var buttonWidth = virtualWidth / 6f;
        var buttonHeight = virtualWidth / 30f;
        var paddingX = virtualWidth / 80f;
        var paddingY = virtualWidth / 80f;

        var buttonTargetPixels = new Rectangle(0, 0, (int)buttonWidth, (int)buttonHeight);
        var buttonPadding = new Point((int)paddingX, (int)paddingY);

        var buttonPositionX = virtualWidth / 2f;
        var buttonPositionY = panelBottom + virtualHeight * 0.05f;

        mButtons.Add(
            mServices.UiFactory.CreateButtonWithSprite(
                world: mUiWorld,
                position: new Vector2(buttonPositionX, buttonPositionY),
                text: "Return",
                onClick: () => mStateManager.PopState(),
                targetPixels: buttonTargetPixels,
                padding: buttonPadding
            )
        );
    }
}