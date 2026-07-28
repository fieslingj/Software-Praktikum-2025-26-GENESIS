using Arch.Core;
using Genesis.Architecture;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Systems;
using System;
using System.Collections.Generic;
using Genesis.Gameplay.Components.Inventory;
using Microsoft.Xna.Framework.Content;
using Genesis.Gameplay.Components.Purchase;
using Genesis.Gameplay.Components.UI;
using Genesis.Gameplay.Definitions;
using Genesis.Gameplay.Extensions;
using Genesis.GameStates.Overlays;

namespace Genesis.UI;

/// <summary>
/// Manages the Entity Component System (ECS) World dedicated to the Head-Up Display (HUD).
/// </summary>
public class HudWorld : IDisposable
{
    private static World sInstanceWorld;
    public World EcsWorld { get; }

    private GameServices mServices;
    private GameStateManager mStateManager;

    // HUD assets
    private SpriteFont mHudFont;

    // Inventory button assets
    private Texture2D mBackpackOpenTexture;
    private Texture2D mBackpackClosedTexture;
    private Entity mInventoryButtonEntity;

    // References to text entities for counters
    private Entity mCoinTextEntity;
    private Entity mAmmoTextEntity;
    private Entity mRunTimerTextEntity;

    // References to progress bar entities
    private Entity mHealthBarEntity;
    private Entity mStaminaBarEntity;
    private Entity mBloodlustBarEntity;
    private Entity mBloodlustTextEntity;

    private readonly List<(Entity Icon, Entity Text, Entity CooldownOverlay, Entity Bar)> mUiSlotEntities = [];   
    private readonly List<Entity> mSlotBackgrounds = [];

    private const int SlotSize = 64;
    private const int SlotGap = 15;
    private const int InventoryButtonSize = 64;
    private const int InventoryButtonMargin = 20;

    // Query to retrieve player's current stats (Health and Stamina) from the game world
    private static readonly QueryDescription sPlayerStatsQuery = new QueryDescription()
        .WithAll<PlayerTagComponent, HealthComponent, StaminaComponent, CoinsComponent, AmmoComponent,
            HotbarComponent, BloodlustTrackerComponent, MutantTypeComponent, AttackCooldownComponent>();

    private Texture2D mPixelTexture;

    public HudWorld(GameServices services, ScreenService screen, GameStateManager stateManager)
    {
        mServices = services;
        mStateManager = stateManager;
        // Create the ECS World for HUD UI elements.
        EcsWorld = World.Create();
        sInstanceWorld = EcsWorld;

        mPixelTexture = new Texture2D(screen.Graphics, 1, 1);
        mPixelTexture.SetData([Color.White]);
        CreateHudElements(services.Content);
    }

    /// <summary>
    /// Creates and initializes all HUD entities (Progress Bars, images and text).
    /// </summary>
    private void CreateHudElements(ContentManager content)
    {
        // Load HUD
        mHudFont = content.Load<SpriteFont>("Fonts/HudFont");

        const int barWidth = 250;
        const int barHeight = 25;
        Rectangle barBounds = new Rectangle(0, 0, barWidth, barHeight);

        // Create health bar entity
        Vector2 healthBarPosition = new Vector2(10, 15);

        mHealthBarEntity = EcsWorld.Create();
        EcsWorld.Add(mHealthBarEntity,
            new PositionComponent { Value = healthBarPosition },
            new ProgressBarComponent
            {
                BackgroundBounds = barBounds,
                ForegroundColor = Color.Red,
                Max = -1f,
                Current = -1f,
                IsActive = true
            }
        );

        // Create stamina bar entity
        Vector2 staminaBarPosition = new Vector2(10, 60);

        mStaminaBarEntity = EcsWorld.Create();
        EcsWorld.Add(mStaminaBarEntity,
            new PositionComponent { Value = staminaBarPosition },
            new ProgressBarComponent
            {
                BackgroundBounds = barBounds,
                ForegroundColor = Color.LightGreen,
                Max = -1f,
                Current = -1f,
                IsActive = true
            }
        );

        // Create bloodthirst tracker bar entity
        var bloodlustBarPosition = new Vector2(10, 100);

        mBloodlustBarEntity = mServices.UiFactory.MarkAsStaticUi(EcsWorld, EcsWorld.Create());
        EcsWorld.Add(mBloodlustBarEntity,
            new PositionComponent { Value = bloodlustBarPosition },
            new ProgressBarComponent
            {
                BackgroundBounds = barBounds,
                ForegroundColor = Color.DarkRed,
                Max = -1f,
                Current = -1f,
                IsActive = true
            }
        );

        // Text labels next to the bars
        const float centerYOffset = barHeight / 2f;

        mServices.UiFactory.CreateText(
            world: EcsWorld,
            position: healthBarPosition + new Vector2(barWidth + 10, centerYOffset),
            text: "HP",
            color: Color.White,
            font: mHudFont,
            alignment: TextAlignment.MiddleLeft
        );

        mServices.UiFactory.CreateText(
            world: EcsWorld,
            position: staminaBarPosition + new Vector2(barWidth + 10, centerYOffset),
            text: "STA",
            color: Color.White,
            font: mHudFont,
            alignment: TextAlignment.MiddleLeft
        );

        mBloodlustTextEntity = mServices.UiFactory.CreateText(
            world: EcsWorld,
            position: bloodlustBarPosition + new Vector2(barWidth + 10, centerYOffset),
            text: "BL",
            color: Color.White,
            font: mHudFont,
            alignment: TextAlignment.MiddleLeft
        );


        const int iconSize = 40;
        const int iconTextGap = 10;
        const int verticalGap = 40;
        var currentPos = new Vector2(10 + iconSize / 2f, 500);

        // Coins
        var coinTexture = content.Load<Texture2D>("Sprites/Icons/Coin");
        var coinScale = (float)iconSize / Math.Max(coinTexture.Width, coinTexture.Height);
        var coinEntity = mServices.UiFactory.MarkAsStaticUi(EcsWorld, EcsWorld.Create());
        EcsWorld.Add(coinEntity,
            new PositionComponent(currentPos),
            new SpriteComponent(
                coinTexture,
                coinTexture.Bounds,
                layerDepth: 0f,
                scale: coinScale
            )
        );

        var coinTextPos = new Vector2(currentPos.X + (iconSize / 2f) + iconTextGap, currentPos.Y);

        mCoinTextEntity = mServices.UiFactory.CreateText(
            world: EcsWorld,
            position: coinTextPos,
            text: "0",
            color: Color.Gold,
            font: mHudFont,
            alignment: TextAlignment.MiddleLeft
        );

        currentPos.Y += verticalGap;

        // Ammo
        var ammoTexture = content.Load<Texture2D>("Sprites/Weapons/Projectile_zugeschnitten");
        var ammoScale = (float)iconSize / Math.Max(ammoTexture.Width, ammoTexture.Height);

        var ammoEntity = mServices.UiFactory.MarkAsStaticUi(EcsWorld, EcsWorld.Create());
        EcsWorld.Add(ammoEntity,
            new PositionComponent(currentPos),
            new SpriteComponent(
                ammoTexture,
                ammoTexture.Bounds,
                layerDepth: 0f,
                scale: ammoScale
            )
        );

        var ammoTextPos = new Vector2(currentPos.X + (iconSize / 2f) + iconTextGap, currentPos.Y);

        mAmmoTextEntity = mServices.UiFactory.CreateText(
            world: EcsWorld,
            position: ammoTextPos,
            text: "0",
            color: Color.White,
            font: mHudFont,
            alignment: TextAlignment.MiddleLeft
        );

        // Create Hotbar
        var slotBounds = new Rectangle(0, 0, SlotSize, SlotSize);

        for (int i = 0; i < 5; i++)
        {
            // Create slot box entity.
            var centerPosition = GetUiSlotPosition(i);
            var slotBoxEntity = mServices.UiFactory.MarkAsStaticUi(EcsWorld, EcsWorld.Create());
            var slotIndex = i;
            EcsWorld.Add(slotBoxEntity,
                new PositionComponent(centerPosition),
                new SpriteComponent(mPixelTexture, slotBounds, 0.2f)
                {
                    mColor = new Color(80, 80, 80, 200)
                },
                new ButtonComponent(slotBounds, () => ActivateHotbarSlot(mServices.World, slotIndex))
            );
            mSlotBackgrounds.Add(slotBoxEntity);

            // Icon entity
            var iconEntity = mServices.UiFactory.MarkAsStaticUi(EcsWorld, EcsWorld.Create());
            EcsWorld.Add(iconEntity,
                new PositionComponent(centerPosition),
                new SpriteComponent(null, Rectangle.Empty, 0.8f)
            );

            // Text entity
            var textPosition = new Vector2(
                centerPosition.X + SlotSize / 2f - 4,
                centerPosition.Y + SlotSize / 2f - 8
            );

            var textEntity = EcsWorld.Create();
            EcsWorld.Add(textEntity,
                new PositionComponent(textPosition),
                new TextComponent("", mHudFont, Color.White, TextAlignment.MiddleRight)
            );
            
            // Cooldown Overlay Entity
            var cooldownOverlayEntity = mServices.UiFactory.MarkAsStaticUi(EcsWorld, EcsWorld.Create());
            EcsWorld.Add(cooldownOverlayEntity,
                new PositionComponent(centerPosition),
                new ProgressPieChartComponent()
            );
            
            // Lifetime Progress Bar
            const int itemBarHeight = 6;
            const int itemBarPadding = 4;
            const int itemBarWidth = SlotSize - (itemBarPadding * 2);
            var itemProgressBarBounds = new Rectangle(0, 0, itemBarWidth, itemBarHeight);
            var barPosition = new Vector2(
                centerPosition.X - (SlotSize / 2f) + itemBarPadding,
                centerPosition.Y + (SlotSize / 2f) - itemBarHeight - itemBarPadding
            );
            var barEntity = mServices.UiFactory.MarkAsStaticUi(EcsWorld, EcsWorld.Create());
            EcsWorld.Add(barEntity,
                new PositionComponent(barPosition),
                new ProgressBarComponent
                {
                    BackgroundBounds = itemProgressBarBounds,
                    ForegroundColor = Color.LightGreen,
                    Max = 100f,
                    Current = 0f,
                    IsActive = false
                }
            );

            mUiSlotEntities.Add((iconEntity, textEntity, cooldownOverlayEntity, barEntity));
        }

        // Create Inventory Button
        mBackpackClosedTexture = content.Load<Texture2D>("Sprites/Buttons/backpack_closed");
        mBackpackOpenTexture = content.Load<Texture2D>("Sprites/Buttons/backpack_open");

        // Position: bottom-right corner with margin
        var buttonPosition = new Vector2(
            ScreenService.VirtualWidth - InventoryButtonMargin - InventoryButtonSize / 2f,
            ScreenService.VirtualHeight - InventoryButtonMargin - InventoryButtonSize / 2f
        );

        var buttonBounds = new Rectangle(0, 0, InventoryButtonSize, InventoryButtonSize);

        mInventoryButtonEntity = mServices.UiFactory.MarkAsStaticUi(EcsWorld, EcsWorld.Create());
        EcsWorld.Add(mInventoryButtonEntity,
            new ButtonComponent(buttonBounds, OnInventoryButtonClicked),
            new PositionComponent(buttonPosition),
            new SpriteComponent(
                mBackpackClosedTexture,
                new Rectangle(0, 0, mBackpackClosedTexture.Width, mBackpackClosedTexture.Height),
                layerDepth: 0f,
                scale: (float)InventoryButtonSize /
                       Math.Max(mBackpackClosedTexture.Width, mBackpackClosedTexture.Height)
            )
        );

        // Position for the run time, top right
        var timerPosition = new Vector2(ScreenService.VirtualWidth - 150, 50);

        mRunTimerTextEntity = mServices.UiFactory.CreateText(
            world: EcsWorld,
            position: timerPosition,
            text: "00:00",
            color: Color.White,
            font: mHudFont,
            alignment: TextAlignment.TopRight // Right-aligned so that the text grows to the left
        );
    }

    /// <summary>
    /// Synchronizes player statistics from the Game World into the HUD World's ProgressBar components.
    /// </summary>
    public void Update(World gameWorld, GameTime gameTime)
    {
        float currentStamina = 0f;
        float currentHealth = 0f;
        float maxStamina = 0f;
        float maxHealth = 0f;
        float currentBloodlust = 0f;
        float maxBloodlust = 0f;
        int currentCoins = 0;
        int currentAmmo = 0;
        bool isBloodlustUnlocked = false;
        double totalRunSeconds = 0;

        // Retrieve current player stats from the GameWorld
        gameWorld.Query(in sPlayerStatsQuery,
            (ref StaminaComponent stamina, ref HealthComponent health, ref CoinsComponent coins, ref AmmoComponent ammo,
                ref HotbarComponent hotbar, ref BloodlustTrackerComponent bloodlust, ref MutantTypeComponent mutant, ref AttackCooldownComponent attackCooldown) =>
            {
                currentStamina = stamina.Current;
                currentHealth = health.Current;
                maxStamina = stamina.Max;
                maxHealth = health.Max;
                currentCoins = coins.CurrentAmount;
                currentAmmo = ammo.Current;
                isBloodlustUnlocked = bloodlust.IsUnlocked;

                if (!isBloodlustUnlocked)
                {
                    currentBloodlust = bloodlust.CurrentDamageSum(gameWorld.GetCurrentRunTimeSeconds());
                    maxBloodlust = BloodlustTrackerComponent.DamageTarget;
                }

                
                var cooldownProgress = MathHelper.Clamp(attackCooldown.CurrentTime / attackCooldown.Delay, 0f, 1f);
                var isOnCooldown = cooldownProgress < 1.0f;
                for (var i = 0; i < 5; i++)
                {
                    if (i >= mUiSlotEntities.Count)
                    {
                        continue;
                    }

                    var (iconEntity, textEntity, overlayEntity, barEntity) = mUiSlotEntities[i];

                    ref var uiSprite = ref EcsWorld.Get<SpriteComponent>(iconEntity);
                    ref var uiText = ref EcsWorld.Get<TextComponent>(textEntity);
                    ref var pieChart = ref EcsWorld.Get<ProgressPieChartComponent>(overlayEntity);
                    ref var uiBar = ref EcsWorld.Get<ProgressBarComponent>(barEntity);

                    if (hotbar.Slots[i] is { } itemEntity && gameWorld.IsAlive(itemEntity))
                    {
                        // A. Slot is assigned
                        ref var item = ref gameWorld.Get<ItemIdentificationComponent>(itemEntity);

                        // Update the sprite
                        var itemIconTexture = mServices.ItemAssets.GetIcon(item.mType);
                        if (itemIconTexture != null)
                        {
                            uiSprite.SpriteSheet = itemIconTexture;
                            uiSprite.SourceRect = itemIconTexture.Bounds;
                            uiSprite.Origin = new Vector2((float)itemIconTexture.Width / 2,
                                (float)itemIconTexture.Height / 2);

                            if (!bloodlust.IsUnlocked && item.mType==PlayerDefinitions.Get(mutant.Type).SpecialItem)
                            {
                                uiSprite.mColor = Color.Black * 0.5f;
                            }
                            else
                            {
                                // Cooldown pie chart and color
                                if (hotbar.ActiveSlot == i && isOnCooldown)
                                {
                                    uiSprite.mColor = Color.Black * 0.5f;
                                    pieChart.mIsActive = true;
                                    pieChart.mProgress = cooldownProgress;
                                }
                                else
                                {
                                    uiSprite.mColor = Color.White; 
                                    pieChart.mIsActive = false;
                                }
                            }

                            float originalSize = Math.Max(itemIconTexture.Width, itemIconTexture.Height);
                            uiSprite.mScale = 0.9f * SlotSize / originalSize;
                        }
                        else
                        {
                            uiSprite.SpriteSheet = null;
                        }

                        // Update the text
                        if (gameWorld.Has<ItemStackComponent>(itemEntity))
                        {
                            var count = gameWorld.Get<ItemStackComponent>(itemEntity).mCount;
                            uiText.Text = count > 0 ? count.ToString() : "";
                        }
                        else
                        {
                            uiText.Text = "";
                        }
                        if (gameWorld.Has<LifeTimeComponent>(itemEntity))
                        {
                            var lifetime = gameWorld.Get<LifeTimeComponent>(itemEntity);
                            uiBar.IsActive = true;
                            uiBar.Max = (float)lifetime.InitialLifeTimeSeconds;
                            uiBar.Current = (float)lifetime.RemainingLifeTimeSeconds;

                            // Farbverlauf Logik
                            var ratio = uiBar.Max > 0 ? uiBar.Current / uiBar.Max : 0;
                            uiBar.ForegroundColor = ratio switch
                            {
                                > 0.5f => Color.LightGreen,
                                > 0.2f => Color.Orange,
                                _ => Color.Red
                            };
                        }
                        else
                        {
                            uiBar.IsActive = false;
                        }
                    }
                    else
                    {
                        // B. Slot ist empty

                        // Deactivate Sprite and empty text
                        uiSprite.SpriteSheet = null;
                        uiText.Text = "";
                        pieChart.mIsActive = false;
                        uiBar.IsActive = false;
                    }
                }

                // Color the backgrounds. If active slots green, if inactive grey.
                for (var i = 0; i < 5; i++)
                {
                    ref var backgroundSprite = ref EcsWorld.Get<SpriteComponent>(mSlotBackgrounds[i]);
                    if (hotbar.ActiveSlot == i)
                    {
                        backgroundSprite.mColor = Color.Green;
                    }
                    else
                    {
                        backgroundSprite.mColor = new Color(80, 80, 80, 200);
                    }
                }
            });

        gameWorld.Query(new QueryDescription().WithAll<RunTimerComponent>(), (ref RunTimerComponent timer) =>
        {
            totalRunSeconds = timer.TotalSeconds;
        });

        // Update the ProgressBarComponent for health
        if (EcsWorld.IsAlive(mHealthBarEntity))
        {
            ref var bar = ref EcsWorld.Get<ProgressBarComponent>(mHealthBarEntity);
            bar.Max = maxHealth;
            bar.Current = currentHealth;
        }

        // Update the ProgressBarComponent for stamina
        if (EcsWorld.IsAlive(mStaminaBarEntity))
        {
            ref var bar = ref EcsWorld.Get<ProgressBarComponent>(mStaminaBarEntity);
            bar.Max = maxStamina;
            bar.Current = currentStamina;
        }

        // Bloodlust
        if (isBloodlustUnlocked)
        {
            if (EcsWorld.IsAlive(mBloodlustBarEntity))
            {
                EcsWorld.Destroy(mBloodlustBarEntity);
            }
            if (EcsWorld.IsAlive(mBloodlustTextEntity))
            {
                EcsWorld.Destroy(mBloodlustTextEntity);
            }
        }
        else if (EcsWorld.IsAlive(mBloodlustBarEntity))
        {
            ref var bar = ref EcsWorld.Get<ProgressBarComponent>(mBloodlustBarEntity);
            bar.Max = maxBloodlust;
            bar.Current = currentBloodlust;
        }

        // Update the time
        if (EcsWorld.IsAlive(mRunTimerTextEntity))
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(totalRunSeconds);
            string timerText = string.Format("{0:D2}:{1:D2}", (int)timeSpan.TotalMinutes, timeSpan.Seconds);

            ref var textComp = ref EcsWorld.Get<TextComponent>(mRunTimerTextEntity);
            textComp.Text = timerText;
        }

        // Update the coin amount text
        EcsWorld.Get<TextComponent>(mCoinTextEntity).Text = currentCoins.ToString();

        // Update the ammo amount text
        EcsWorld.Get<TextComponent>(mAmmoTextEntity).Text = currentAmmo.ToString();

        // Update the Hotbar

    }

    /// <summary>
    /// Draws all HUD entities using the DrawSystem.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, DrawSystem drawSystem)
    {
        // Draw the HUD World. isUiWorld=true suppresses debug drawings.
        drawSystem.Draw(EcsWorld, spriteBatch);
    }

    /// <summary>
    /// Disposes of unmanaged resources (the Arch.Core.World) when the state is exited.
    /// </summary>
    public void Dispose()
    {
        EcsWorld.Dispose();
    }

    private Vector2 GetUiSlotPosition(int i)
    {
        const int width = 5 * SlotSize + 4 * SlotGap;
        const int height = 2 * SlotSize + 1 * SlotGap;

        var startX = ScreenService.VirtualWidth / 2f - width / 2f;
        var startY = ScreenService.VirtualHeight - height / 2f;

        var row = i / 5;
        var col = i % 5;

        return new Vector2(
            // start + column offset + half slot size
            startX + col * (SlotSize + SlotGap) + SlotSize / 2f,
            // start + row offset + half slot size
            startY + row * (SlotSize + SlotGap) + SlotSize / 2f
        );
    }

    private static void ActivateHotbarSlot(World gameWorld, int slotIndex)
    {
        gameWorld.Query(new QueryDescription().WithAll<HotbarComponent, PlayerTagComponent>(),
            (ref HotbarComponent hotbar) => { hotbar.ActiveSlot = slotIndex; });
    }

    /// <summary>
    /// Callback when inventory button is clicked.
    /// Opens the inventory and switches to the "open" sprite.
    /// </summary>
    private void OnInventoryButtonClicked()
    {
        if (mStateManager.IsTopState<InventoryState>() || mStateManager.IsTopState<PurchaseMenuState>())
        {
            SetInventoryOpen(false);
            mStateManager.PopState();
        }
        else
        {
            SetInventoryOpen(true);
            mStateManager.PushState(new InventoryState());
        }
    }

    /// <summary>
    /// Updates the inventory button sprite based on whether inventory is open.
    /// Call this from IngameState when inventory state changes.
    /// </summary>
    public void SetInventoryOpen(bool isOpen)
    {
        ref var sprite = ref EcsWorld.Get<SpriteComponent>(mInventoryButtonEntity);

        if (isOpen)
        {
            sprite.SpriteSheet = mBackpackOpenTexture;
            sprite.SourceRect = new Rectangle(0, 0, mBackpackOpenTexture.Width, mBackpackOpenTexture.Height);
            sprite.mScale = (float)InventoryButtonSize /
                            Math.Max(mBackpackOpenTexture.Width, mBackpackOpenTexture.Height);
        }
        else
        {
            sprite.SpriteSheet = mBackpackClosedTexture;
            sprite.SourceRect = new Rectangle(0, 0, mBackpackClosedTexture.Width, mBackpackClosedTexture.Height);
            sprite.mScale = (float)InventoryButtonSize /
                            Math.Max(mBackpackClosedTexture.Width, mBackpackClosedTexture.Height);
        }
    }

    public static bool IsMouseOverGui(Vector2 mousePos)
    {
        if (sInstanceWorld == null) { return false; }
        var isOver = false;

        var buttonQuery = new QueryDescription().WithAll<PositionComponent, ButtonComponent>();
        sInstanceWorld.Query(in buttonQuery, (ref PositionComponent pos, ref ButtonComponent btn) =>
        {
            if (isOver) { return; }
            var rect = new Rectangle(
                (int)(pos.Value.X - btn.Bounds.Width / 2f),
                (int)(pos.Value.Y - btn.Bounds.Height / 2f),
                btn.Bounds.Width,
                btn.Bounds.Height
            );

            if (rect.Contains(mousePos)) { isOver = true; }
        });
        return isOver;
    }
}