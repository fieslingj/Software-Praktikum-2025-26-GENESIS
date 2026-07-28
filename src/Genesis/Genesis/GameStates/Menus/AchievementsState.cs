#nullable enable
using System;
using System.Collections.Generic;
using Arch.Core;
using Genesis.Architecture;
using Genesis.Architecture.Audio;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Systems;
using Genesis.Persistence.Meta;
using Genesis.Simulation.Achievements;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Genesis.GameStates.Menus;

public class AchivementsState : IGameState
{
    private const float VirtualWidth = ScreenService.VirtualWidth;
    private const float VirtualHeight = ScreenService.VirtualHeight;
    
    private const float Radius = VirtualHeight * 0.25f;
    private const float IconAreaWidth = VirtualWidth / 8f;
    private const float IconAreaHeight = VirtualHeight / 6.5f;
    
    
    private sealed class AchievementDefinition
    {
        public string Name { get; }
        public string Description { get; }

        public AchievementDefinition(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }

    private enum AchievementId
    {
        Bloodthirsty = 0,
        LaserGazer = 1,
        HeavyMetal = 2,
        GoingViral = 3,
        Speedrunner = 4
    }

    private GameStateManager mStateManager = null!;
    private GameServices mServices = null!;
    private ScreenService mScreenService = null!;

    private World mUiWorld = null!;

    private readonly List<Entity> mButtons = new();

    // Achievements list
    private readonly List<AchievementDefinition> mAchievements = new()
    {
        new("Bloodthirsty",
            "Unlock the Bloodlust mechanic \n" +
            "by dealing high damage in a short period of time."),
        new("Laser Gazer",
            "Unlock the special attack Laser Arm for the first time."),
        new("Heavy Metal",
            "Unlock the special attack Arms of Steel for the first time."),
        new("Going Viral",
            "Unlock the special attack Acid Spit for the first time."),
        new("Speedrunner",
            $"Defeat the CEO in under {AchievementUnlocker.SpeedrunTimeLimitMinutes} minutes."),
    };

    // Hover-Tooltip state
    private Entity? mTooltipEntity;
    private Entity? mTooltipBackground;
    private AchievementDefinition? mHoveredAchievement;
    
    private Texture2D mPixelTexture = null!;

    private GlobalAchievementsData Achievements => mServices.MetaData.Achievements;

    private bool IsBloodthirstyUnlocked => Achievements.IsUnlocked(AchievementIds.BloodRage);
    private bool IsLaserGazerUnlocked => Achievements.IsUnlocked(AchievementIds.LaserGazer);
    private bool IsHeavyMetalUnlocked => Achievements.IsUnlocked(AchievementIds.HeavyMetal);
    private bool IsGoingViralUnlocked => Achievements.IsUnlocked(AchievementIds.GoingViral);
    private bool IsSpeedrunnerUnlocked => Achievements.IsUnlocked(AchievementIds.Speedrunner);

    // Achievement unlock check
    private bool IsAchievementUnlocked(int index)
    {
        var id = (AchievementId)index;

        return id switch
        {
            AchievementId.Bloodthirsty => IsBloodthirstyUnlocked,
            AchievementId.LaserGazer => IsLaserGazerUnlocked,
            AchievementId.HeavyMetal => IsHeavyMetalUnlocked,
            AchievementId.GoingViral => IsGoingViralUnlocked,
            AchievementId.Speedrunner => IsSpeedrunnerUnlocked,
            _ => false
        };
    }

    private string GetAchievementIconPath(int index)
    {
        var id = (AchievementId)index;

        // Lock icon if not unlocked
        if (!IsAchievementUnlocked(index)) { return "Sprites/Buttons/Lock"; }

        // If unlocked, return specific icon path
        return id switch
        {
            AchievementId.Bloodthirsty => "Sprites/Icons/BlutRauschIcon",
            AchievementId.LaserGazer => "Sprites/Icons/LaserArm",
            AchievementId.HeavyMetal => "Sprites/Icons/Iron",
            AchievementId.GoingViral => "Sprites/Icons/AcidSpit",
            AchievementId.Speedrunner => "Sprites/Icons/SpeedrunIcon",
            _ => "Sprites/Buttons/Lock"
        };
    }

    public void Initialize(GameStateManager manager, GameServices services, ScreenService screen, AudioService sound)
    {
        mStateManager = manager;
        mServices = services;
        mScreenService = screen;
    }

    public void Enter()
    {
        mUiWorld = World.Create();
        mPixelTexture = new Texture2D(mScreenService.Graphics, 1, 1);
        mPixelTexture.SetData([Color.White]);
        BuildUi();
    }

    public void Exit()
    {
        mUiWorld.Dispose();
        mPixelTexture?.Dispose();
    }

    public void Pause() { }

    public void Resume() { }

    public void HandleInput(InputService input)
    {
        if (input.IsActionPressed(InputAction.Pause))
        {
            mStateManager?.PopState();
            return;
        }

        // Update tooltip based on mouse position
        UpdateTooltip(input);

        // Run input systems
        mServices?.Systems.HandleInput(mUiWorld, input);
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
        var textFont = mServices.Content.Load<SpriteFont>("Fonts/GenesisFont");
        mButtons.Clear();

        mUiWorld = mUiWorld ?? World.Create();

        // Load background texture for achievement icons
        var backTexture = mServices.Content.Load<Texture2D>("Sprites/Buttons/back");

        // Scenter for circle distribution
        var center = new Vector2(VirtualWidth / 2f, VirtualHeight / 2f);

        // Height offset for text below icons
        var textOffsetY = VirtualHeight * 0.045f;

        for (int i = 0; i < mAchievements.Count; i++)
        {
            var achievement = mAchievements[i];

            // Turn angle for circle distribution
            var angle = -MathF.PI / 2f + i * (MathF.Tau / mAchievements.Count);
            var iconCenter = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * Radius;

            // Move icons slightly upwards for better visual balance
            iconCenter.Y -= VirtualHeight * 0.03f;

            // Background image for icon
            var backScale = Math.Min(
                IconAreaWidth / backTexture.Width,
                IconAreaHeight / backTexture.Height
            );

            var backEntity = mServices.UiFactory.CreateImage(
                world: mUiWorld,
                position: iconCenter,
                texture: backTexture,
                sourceRect: null,
                depth: 0.4f
            );

            if (mUiWorld.Has<SpriteComponent>(backEntity))
            {
                ref var backSprite = ref mUiWorld.Get<SpriteComponent>(backEntity);
                backSprite.mScale = backScale;
            }

            // Set achievement icon (locked or unlocked)
            var iconPath = GetAchievementIconPath(i);
            var iconTexture = mServices.Content.Load<Texture2D>(iconPath);

            var scaleIcon = 0.70f;

            var iconScale = Math.Min(
                (IconAreaWidth * 0.8f) / iconTexture.Width,
                (IconAreaHeight * 0.8f) / iconTexture.Height
            ) * scaleIcon;

            var iconEntity = mServices.UiFactory.CreateImage(
                world: mUiWorld,
                position: iconCenter,
                texture: iconTexture,
                sourceRect: null,
                depth: 0.45f
            );
            if (mUiWorld.Has<SpriteComponent>(iconEntity))
            {
                ref var iconSprite = ref mUiWorld.Get<SpriteComponent>(iconEntity);
                iconSprite.mScale = iconScale;
            }

            // Set achievement name text below icon
            var textPosition = iconCenter + new Vector2(0f, IconAreaHeight / 2f + textOffsetY);

            var nameEntity = mServices.UiFactory.CreateText(
                world: mUiWorld,
                position: textPosition,
                text: achievement.Name,
                font: textFont,
                color: Color.Black,
                alignment: TextAlignment.MiddleCenter
            );
            mUiWorld.Get<TextComponent>(nameEntity).LayerDepth = 0.7f;
        }

        // Return button parameters
        var buttonWidth = VirtualWidth / 6f;
        var buttonHeight = VirtualWidth / 30f;
        var paddingX = VirtualWidth / 80f;
        var paddingY = VirtualWidth / 80f;

        var buttonTargetPixels = new Rectangle(0, 0, (int)buttonWidth, (int)buttonHeight);
        var buttonPadding = new Point((int)paddingX, (int)paddingY);

        var buttonPositionX = VirtualWidth / 2f;
        var buttonPositionY = center.Y + Radius + VirtualHeight * 0.15f;

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

    private void UpdateTooltip(InputService input)
    {
        var textFont = mServices.Content.Load<SpriteFont>("Fonts/GenesisFont");

        var rawMousePos = input.GetMousePosition();
        var mouseVirtual = mScreenService.Adapter.PointToScreen(rawMousePos.X, rawMousePos.Y).ToVector2();

        // Not hovering any achievement
        AchievementDefinition? hovered = null;
        int hoveredIndex = -1;

        // Rendering parameters
        var virtualWidth = (float)ScreenService.VirtualWidth;
        var virtualHeight = (float)ScreenService.VirtualHeight;

        var center = new Vector2(virtualWidth / 2f, virtualHeight / 2f);

        for (int i = 0; i < mAchievements.Count; i++)
        {
            var angle = -MathF.PI / 2f + i * (MathF.Tau / mAchievements.Count);
            var iconCenter = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * Radius;

            iconCenter.Y -= virtualHeight * 0.03f;

            var rect = new Rectangle(
                (int)(iconCenter.X - IconAreaWidth / 2f),
                (int)(iconCenter.Y - IconAreaHeight / 2f),
                (int)IconAreaWidth,
                (int)IconAreaHeight
            );

            if (rect.Contains(mouseVirtual))
            {
                hovered = mAchievements[i];
                hoveredIndex = i;
                break;
            }
        }

        if (hovered == null)
        {
            ClearTooltip();

            mHoveredAchievement = null;
            return;
        }

        if (hovered != mHoveredAchievement)
        {
            mHoveredAchievement = hovered;
            ClearTooltip();

            var textSize = textFont.MeasureString(hovered.Description);
            var padding = new Vector2(20f, 20f);
            var boxSize = textSize + (padding * 2f);

            var offset = new Vector2(20f, 20f); 
            var topLeft = mouseVirtual + offset;

            if (topLeft.X + boxSize.X > virtualWidth) { topLeft.X = mouseVirtual.X - boxSize.X - offset.X; }
            if (topLeft.Y + boxSize.Y > virtualHeight) { topLeft.Y = mouseVirtual.Y - boxSize.Y - offset.Y; }
            if (topLeft.X < 0) { topLeft.X = 0; }
            if (topLeft.Y < 0) { topLeft.Y = 0; }

            var centerPos = topLeft + (boxSize / 2f);
            var entity = mUiWorld.Create();
            mTooltipBackground = mServices.UiFactory.MarkAsStaticUi(mUiWorld, entity);
            
            mUiWorld.Add(mTooltipBackground.Value,
                new PositionComponent(centerPos),
                new SpriteComponent(
                    spriteSheet: mPixelTexture,
                    sourceRect: new Rectangle(0, 0, (int)boxSize.X, (int)boxSize.Y),
                    layerDepth: 0.8f,
                    scale: 1.0f
                )
                {
                    mColor = new Color(200, 200, 200, 200),
                    Origin = boxSize / 2f 
                });
            var textPosition = centerPos - (textSize / 2f) + new Vector2(0, 12f);

            // Set tooltip color based on state (green = unlocked, red = locked)
            var tooltipColor = (hoveredIndex >= 0 && IsAchievementUnlocked(hoveredIndex))
                ? Color.Green
                : Color.DarkRed;

            mTooltipEntity = mServices.UiFactory.CreateText(
                world: mUiWorld,
                position: textPosition,
                text: hovered.Description,
                font: textFont,
                color: tooltipColor,
                alignment: TextAlignment.TopLeft
            );

            if (!mUiWorld.Has<SpriteComponent>(mTooltipEntity.Value)) {return; }
            ref var textSprite = ref mUiWorld.Get<SpriteComponent>(mTooltipEntity.Value);
            textSprite.LayerDepth = 0.9f;
        }
    }

    private void ClearTooltip()
    {
        if (mTooltipEntity.HasValue)
        {
            mUiWorld.Destroy(mTooltipEntity.Value);
            mTooltipEntity = null;
        }
        if (mTooltipBackground.HasValue)
        {
            mUiWorld.Destroy(mTooltipBackground.Value);
            mTooltipBackground = null;
        }
    }
}