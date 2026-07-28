using System;
using System.Collections.Generic;
using Arch.Core;
using Genesis.Architecture;
using Genesis.Architecture.Audio;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Components.Purchase;
using Genesis.Gameplay.Definitions;
using Genesis.Gameplay.Systems;
using Genesis.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace Genesis.GameStates.Overlays;

public struct ShopSlotComponent(ItemType itemType) { public ItemType mItemType = itemType; }

public class PurchaseMenuState  : IGameState
{
    private World mUiWorld;
    private GameStateManager mStateManager;
    private GameServices mServices;
    private ScreenService mScreenService;

    private Texture2D mPixelTexture;
    private SpriteFont mFont;

    private InventoryUiController mInventoryUi;
    private IHudController mHudController;

    private List<ItemType> mShopItems;
    
    private TooltipRenderer mTooltipRenderer;
    private Vector2 mCurrentMousePosition;

    private Entity mLastHoveredSlot = Entity.Null;
    private List<Entity> mActiveShopTooltipEntities = [];

    private Rectangle mSlotBounds;
    
    private AudioService mAudioService;

    public void Initialize(GameStateManager manager, GameServices services, ScreenService screen, AudioService sound)
    {
        mStateManager = manager;
        mServices = services;
        mScreenService = screen;
        mTooltipRenderer = new TooltipRenderer(services, screen);
        mAudioService = sound;
    }

    public void Enter()
    {
        mShopItems = new List<ItemType>(ItemDefinitions.GetShopItems());

        mUiWorld = World.Create();

        mPixelTexture = new Texture2D(mScreenService.Graphics, 1, 1, false, SurfaceFormat.Color);
        mPixelTexture.SetData([Color.White]);
        mFont = mServices.Content.Load<SpriteFont>("Fonts/HudFont");

        mInventoryUi = new InventoryUiController(mServices, mUiWorld, mScreenService, mAudioService);
        mHudController = mStateManager.GetBelowTopState() as IHudController;

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
        if (input.IsActionPressed(InputAction.Interact)
            || input.IsActionPressed(InputAction.Pause))
        {
            mStateManager.PopState();
            return;
        }
        var rawMousePos = input.GetMousePosition(); 
        var virtualMousePoint = mScreenService.Adapter.PointToScreen(rawMousePos.X, rawMousePos.Y);
        mCurrentMousePosition = virtualMousePoint.ToVector2();
        
        mServices.Systems.Get<ButtonInputSystem>().HandleInput(mUiWorld, input);
        mServices.Systems.Get<HotbarInputSystem>().HandleInput(mServices.World, input);

        mHudController?.HandleHudInput(input);
    }

    public void Update(GameTime gameTime)
    {
        mServices.Systems.Get<PurchaseSystem>().Update(mServices.World, gameTime);
        mServices.Systems.Get<InventorySystem>().Update(mServices.World, gameTime);
        mServices.Systems.Get<RunTimerSystem>().Update(mServices.World, gameTime);
        mInventoryUi.Update(mServices.World, mCurrentMousePosition);
        UpdateShopTooltips();
        mHudController?.UpdateHud(gameTime);
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        // Draws the game in paused state
        mStateManager.DrawBelowTop(gameTime, spriteBatch);

        // Draws the purchase menu on top
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
        var virtualWidth = (float)ScreenService.VirtualWidth;
        var virtualHeight = (float)ScreenService.VirtualHeight;

        // Create background entity.
        var bgEntity = mServices.UiFactory.MarkAsStaticUi(mUiWorld, mUiWorld.Create());

        mUiWorld.Add(bgEntity,
            new PositionComponent(new Vector2(virtualWidth / 2f, virtualHeight / 2f)),
            new SpriteComponent(
                spriteSheet: mPixelTexture,
                sourceRect: new Rectangle(0, 0,  (int)(virtualWidth * 0.8f), (int)(virtualHeight * 0.8f)),
                layerDepth: 0f,
                scale: 1.0f
            )
            {
                mColor = new Color(0, 0, 0, 200)
            });

        // Shop grid
        BuildShopGrid(virtualWidth, virtualHeight);

        // Inventory grid
        const int inventoryColumns = 3;
        const float inventoryStartX = ScreenService.VirtualWidth * 0.65f;
        const float totalInventoryHeight = 5 * InventoryUiController.SlotSize + 4 * InventoryUiController.Gap;
        const float inventoryStartY = (ScreenService.VirtualHeight - totalInventoryHeight) / 2f;

        // Build UI
        mInventoryUi.BuildUi(
            startPosition: new Vector2(inventoryStartX, inventoryStartY),
            columns: inventoryColumns,
            onSlotClicked: (index) => InventorySystem.AssignItemToHotbar(mServices.World, index)
        );

        // Return button
        var btnWidth = virtualWidth / 6f;
        var btnHeight = virtualWidth / 30f;
        mServices.UiFactory.CreateButtonWithSprite(
            world: mUiWorld,
            position: new Vector2(virtualWidth / 2f, virtualHeight * 0.85f),
            text: "Return",
            onClick: () => mStateManager.PopState(),
            targetPixels: new Rectangle(0, 0, (int)btnWidth, (int)btnHeight),
            padding: new Point(5, 5));
    }

   private void BuildShopGrid(float screenWidth, float screenHeight)
    {
        const int columns = 2;
        const float slotSize = 64f;
        const float gap = 18f;

        var startX = screenWidth * 0.25f;

        var rows = (int)Math.Ceiling(mShopItems.Count / (float)columns);
        var totalGridHeight = (rows * slotSize) + ((rows - 1) * gap);
        var startY = (screenHeight - totalGridHeight) / 2f;

        mSlotBounds = new Rectangle(0, 0, (int)slotSize, (int)slotSize);

        for (var i = 0; i < mShopItems.Count; i++)
        {
            var itemType = mShopItems[i];

            var col = i % columns;
            var row = i / columns;

            // position in the ui world
            var centerPosition = new Vector2(
                startX + (col * (slotSize + gap)),
                startY + (row * (slotSize + gap))
            );

            // Background and button entity
            var bgEntity = mServices.UiFactory.MarkAsStaticUi(mUiWorld, mUiWorld.Create());
            mUiWorld.Add(bgEntity,
                new PositionComponent(centerPosition),
                new SpriteComponent(mPixelTexture, new Rectangle(0, 0, (int)slotSize, (int)slotSize), 0.7f)
                {
                    mColor = new Color(80, 80, 80, 200),
                    Origin = new Vector2(slotSize / 2f, slotSize / 2f)
                },
                new ButtonComponent(mSlotBounds, () =>
                {
                    HandlePurchase(itemType);
                }),
                new ShopSlotComponent(itemType)
            );

            // Icon Entity
            var iconTexture = mServices.ItemAssets.GetIcon(itemType);
            if (iconTexture != null)
            {
                var iconEntity = mServices.UiFactory.MarkAsStaticUi(mUiWorld, mUiWorld.Create());
                var scale = (slotSize * 0.9f) / Math.Max(iconTexture.Width, iconTexture.Height);

                mUiWorld.Add(iconEntity,
                    new PositionComponent(centerPosition),
                    new SpriteComponent(iconTexture, iconTexture.Bounds, 0.8f, scale)
                    {
                        Origin = new Vector2(iconTexture.Width / 2f, iconTexture.Height / 2f)
                    },
                    new IsVisibleComponent(),
                    new IgnoreCullingComponent()
                );
            }

            // Text Entity
            var textPosition = new Vector2(
                centerPosition.X + slotSize / 2f - 4,
                centerPosition.Y + slotSize / 2f - 8
            );

            var price = ItemDefinitions.GetPrice(itemType);
            var textEntity = mUiWorld.Create();
            mUiWorld.Add(textEntity,
                new PositionComponent(textPosition),
                new TextComponent($"{price}", mFont, Color.Gold, TextAlignment.MiddleRight, 0.95f)
            );
        }
    }

    private void HandlePurchase(ItemType type)
    {
        var (gameWorld, playerEntity) = GetPlayerContext();
        if (playerEntity != Entity.Null)
        {
            gameWorld.Add(playerEntity, new PurchaseRequestComponent(type));
        }
    }
    
    private void UpdateShopTooltips()
    {
        var currentHoveredEntity = Entity.Null;
        var hoveredItemType = ItemType.None;
        var query = new QueryDescription().WithAll<PositionComponent, ShopSlotComponent>();
        
        var halfWidth = mSlotBounds.Width / 2f;
        var halfHeight = mSlotBounds.Height / 2f;

        mUiWorld.Query(in query, (Entity entity, ref PositionComponent pos, ref ShopSlotComponent slot) =>
        {
            if (currentHoveredEntity != Entity.Null) { return; }

            // Prüfen: Maus innerhalb der Bounds dieses Slots?
            if (mCurrentMousePosition.X < pos.Value.X - halfWidth ||
                mCurrentMousePosition.X > pos.Value.X + halfWidth ||
                mCurrentMousePosition.Y < pos.Value.Y - halfHeight ||
                mCurrentMousePosition.Y > pos.Value.Y + halfHeight) { return; }

            currentHoveredEntity = entity;
            hoveredItemType = slot.mItemType;
        });

        // State Change Check (Haben wir den Slot gewechselt?)
        if (currentHoveredEntity == mLastHoveredSlot) { return; }

        {
            // Remove old tooltip
            foreach (var entity in mActiveShopTooltipEntities)
            {
                mUiWorld.Destroy(entity);
            }
            mActiveShopTooltipEntities.Clear();

            // Create new tooltip if hovered
            if (currentHoveredEntity != Entity.Null && hoveredItemType != ItemType.None)
            {
                var def = ItemDefinitions.Get(hoveredItemType);
                mActiveShopTooltipEntities = mTooltipRenderer.CreateItemTooltip(mUiWorld, def, mCurrentMousePosition);
            }
            mLastHoveredSlot = currentHoveredEntity;
        }
    }

    private (World, Entity) GetPlayerContext()
    {
        var gameWorld = mServices.World;
        Entity playerEntity = Entity.Null;

        gameWorld.Query(new QueryDescription().WithAll<PlayerTagComponent>(),
            (Entity entity) => { playerEntity = entity; });

        return (gameWorld, playerEntity);
    }
}