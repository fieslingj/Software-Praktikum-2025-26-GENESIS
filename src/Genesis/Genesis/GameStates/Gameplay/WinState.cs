using Arch.Core;
using System;
using Genesis.Architecture;
using Genesis.Architecture.Audio;
using Genesis.Architecture.Persistence;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Components.World;
using Genesis.Gameplay.Extensions;
using Genesis.GameStates.Menus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Genesis.Persistence.Run;
using Genesis.Simulation.Achievements;

namespace Genesis.GameStates.Gameplay;

public class WinState : IGameState
{
    private GameStateManager mGameStateManager;
    private ScreenService mScreenService;
    private World mGameWorld;
    private SpriteFont mFont;
    private AchievementUnlocker mAchievementUnlocker;
    private AudioService mSound;

    // Determine how transparent the black rectangle should be.
    // In this case you can see 40% of the original game world.
    private const float MOverlayMaxOpacity = 0.6f;

    // Animation Timers
    private float mTimer;
    private const float FadeInDuration = 2.0f;
    private const float LingerDuration = 3.0f;

    // Overlay color.
    private readonly Color mOverlayColor = Color.Black * 0.85f;

    // "YOU WON" Color
    private readonly Color mGlowColor = Color.Gold * 0.85f;
    private readonly Color mTitleColor = Color.PaleGoldenrod;

    private GameServices mServices;

    public void Initialize(GameStateManager manager, GameServices services, ScreenService screen, AudioService sound)
    {
        mGameStateManager = manager;
        mScreenService = screen;
        mGameWorld = services.World;
        mAchievementUnlocker = new AchievementUnlocker(services);
        mFont = services.Content.Load<SpriteFont>("Fonts/TitleFont");
        mServices = services;
        mSound = sound;
    }

    public void Enter()
    {
        DeleteRun();
        mTimer = 0f;
        var runDuration = mGameWorld.GetCurrentRunDuration();
        mSound.PlayMusic("Sounds/Effects/gamewon", isRepeating: false);

        // Only save statistic and unlock achivements if not Techdemo
        var runStats = mGameWorld.GetResource<RunStatsComponent>();
        if ((runStats is { RunType: (int)RunType.Techdemo } _))
        {
            return;
        }
        SavedStatisticData.Fetch(mGameWorld, StatisticCallingState.Win, mServices);
        mAchievementUnlocker.UnlockSpeedRunnerAchivement(runDuration);
    }

    public void Exit() { }
    public void Pause() { }
    public void Resume() { }

    public void HandleInput(InputService input)
    {
        if (input.IsActionPressed(InputAction.Pause))
        {
            mGameStateManager.ChangeState(new MainMenuState());
            DeleteRun();
        }
    }

    public void Update(GameTime gameTime)
    {
        var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        mTimer += deltaTime;
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var metaData = mGameWorld.GetResource<MetaDataComponent>().Data;
        var alpha = MathHelper.Clamp(mTimer / FadeInDuration, 0.0f, 1.0f);
        var runDuration = mGameWorld.GetCurrentRunDuration();

        spriteBatch.Begin(
            transformMatrix: mScreenService.Adapter.GetScaleMatrix(),
            samplerState: SamplerState.LinearClamp,
            blendState: BlendState.AlphaBlend
        );

        // Background fades in
        spriteBatch.Draw(
            GetPixel(spriteBatch.GraphicsDevice),
            new Rectangle(0, 0, mScreenService.Adapter.VirtualWidth, mScreenService.Adapter.VirtualHeight),
            mOverlayColor * alpha
        );

        // Text plus Text-Shadow fades in
        const string text = "YOU WON";
        var size = mFont.MeasureString(text);
        var center = new Vector2(mScreenService.Adapter.VirtualWidth / 2f, mScreenService.Adapter.VirtualHeight / 2f);
        var pos = center - (size / 2);

        float glowOffset = 2f;
        Vector2[] offsets = {
            new Vector2(-glowOffset, -glowOffset), new Vector2(glowOffset, -glowOffset),
            new Vector2(-glowOffset, glowOffset), new Vector2(glowOffset, glowOffset),
            new Vector2(0, glowOffset), new Vector2(0, -glowOffset)
        };

        foreach (var offset in offsets)
        {
            spriteBatch.DrawString(mFont, text, pos + offset, mGlowColor * alpha);
        }
        // Draw text shadow
        spriteBatch.DrawString(mFont, text, pos, mTitleColor * alpha);

        // Draw Metadata (left column) and This Run (right column) in two symmetrical columns
        var runStats = mGameWorld.GetResource<RunStatsComponent>();
        if (metaData != null && runStats is {})
        {

            var totalDamageDealt = metaData.Statistics.TotalDamageDealt + runStats.DamageDealt;
            var totalDamageTaken = metaData.Statistics.TotalDamageTaken + runStats.DamageTaken;
            var totalDeaths = metaData.Statistics.TotalDeaths;
            var totalEnemiesDefeated = metaData.Statistics.TotalEnemiesDefeated + runStats.EnemiesDefeated;
            var totalPlaytime = metaData.Statistics.TotalPlaytimeSeconds + (float)runDuration.TotalSeconds;
            var formattedPlaytime = FormatPlaytime(totalPlaytime);
            var totalSuccessfulRuns = metaData.Statistics.TotalSuccessfulRuns + 1;

            // Layout parameters
            float scale = 0.12f;
            float headerScale = scale + 0.02f;
            var centerX = center.X;
            float columnSpacing = 40f;
            float columnWidth = 320f;
            var baseY = center.Y + (size.Y / 2f) + 24f;
            var lineHeight = mFont.MeasureString("Ay").Y * scale + 6f;

            var leftX = centerX - columnSpacing - columnWidth;
            var rightX = centerX + columnSpacing;

            // Left: Lifetime / Meta statistics
            spriteBatch.DrawString(mFont, "Lifetime Stats", new Vector2(leftX, baseY), Color.Gold * alpha, 0f, Vector2.Zero, headerScale, SpriteEffects.None, 1f);
            var ly = baseY + lineHeight;
            spriteBatch.DrawString(mFont, $"Total Damage Dealt: {totalDamageDealt}", new Vector2(leftX, ly), Color.White * alpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f); ly += lineHeight;
            spriteBatch.DrawString(mFont, $"Total Damage Taken: {totalDamageTaken}", new Vector2(leftX, ly), Color.White * alpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f); ly += lineHeight;
            spriteBatch.DrawString(mFont, $"Total Deaths: {totalDeaths}", new Vector2(leftX, ly), Color.White * alpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f); ly += lineHeight;
            spriteBatch.DrawString(mFont, $"Total Enemies Defeated: {totalEnemiesDefeated}", new Vector2(leftX, ly), Color.White * alpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f); ly += lineHeight;
            spriteBatch.DrawString(mFont, $"Total Playtime: {formattedPlaytime}", new Vector2(leftX, ly), Color.White * alpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f); ly += lineHeight;
            spriteBatch.DrawString(mFont, $"Total Successful Runs: {totalSuccessfulRuns}", new Vector2(leftX, ly), Color.White * alpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);

            // Right: This run stats
            spriteBatch.DrawString(mFont, "This Run", new Vector2(rightX, baseY), Color.Gold * alpha, 0f, Vector2.Zero, headerScale, SpriteEffects.None, 1f);
            var ry = baseY + lineHeight;
            spriteBatch.DrawString(mFont, $"Damage Dealt: {runStats.DamageDealt}", new Vector2(rightX, ry), Color.White * alpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f); ry += lineHeight;
            spriteBatch.DrawString(mFont, $"Damage Taken: {runStats.DamageTaken}", new Vector2(rightX, ry), Color.White * alpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f); ry += lineHeight;
            spriteBatch.DrawString(mFont, $"Enemies Defeated: {runStats.EnemiesDefeated}", new Vector2(rightX, ry), Color.White * alpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f); ry += lineHeight;
            spriteBatch.DrawString(mFont, $"Playtime: {FormatPlaytime((float)runDuration.TotalSeconds)}", new Vector2(rightX, ry), Color.White * alpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
        }

        spriteBatch.End();
    }

    private Texture2D mPixel;

    private Texture2D GetPixel(GraphicsDevice graphicsDevice)
    {
        if (mPixel is not null) { return mPixel; }

        mPixel = new Texture2D(graphicsDevice, 1, 1);
        mPixel.SetData(new[] { Color.White });
        return mPixel;
    }

    private string FormatPlaytime(float seconds)
    {
        if (seconds <= 0) return "00m 00s"; // Fallback für leere Werte

        var time = TimeSpan.FromSeconds(seconds);

        // Wenn die Zeit über eine Stunde ist, zeigen wir Stunden, Minuten und Sekunden
        if (time.TotalHours >= 1)
        {
            return $"{(int)time.TotalHours:D2}h {time.Minutes:D2}m {time.Seconds:D2}s";
        }

        // Ansonsten reichen Minuten und Sekunden
        return $"{time.Minutes:D2}m {time.Seconds:D2}s";
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
