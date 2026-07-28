using System;
using System.Collections.Generic;
using Arch.Core;
using Genesis.Architecture;
using Genesis.Architecture.Audio;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Components.Inventory;
using Genesis.Gameplay.Definitions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace Genesis.UI;

public class InventoryUiController(GameServices services, World uiWorld, ScreenService screen, AudioService audio) : IDisposable
{
    private readonly List<(Entity Icon, Entity Text, Entity Bar)> mUiSlotEntities = [];

    public const int SlotSize = 64;
    public const int Gap = 18;
    private const int MaxSlots = 15;

    private readonly Texture2D mPixelTexture = CreatePixelTexture(screen.Graphics);
    private readonly SpriteFont mFont = services.Content.Load<SpriteFont>("Fonts/HudFont");
    private static readonly Color sBackgroundBoxColor = new(80, 80, 80, 200);
    
    private readonly TooltipRenderer mTooltipRenderer = new(services, screen);
    private List<Entity> mActiveTooltipEntities = [];
    private int mHoveredSlotIndex = -1;
    
    private AudioService mAudioService = audio;

    private static Texture2D CreatePixelTexture(GraphicsDevice graphics)
    {
        var texture = new Texture2D(graphics, 1, 1);
        texture.SetData([Color.White]);
        return texture;
    }

    public void BuildUi(Vector2 startPosition, int columns, Action<int> onSlotClicked)
    {
        mUiSlotEntities.Clear();
        var slotBounds = new Rectangle(0, 0, SlotSize, SlotSize);

        // Lifetime bar
        const int barHeight = 6;
        const int barPadding = 4;
        const int barWidth = SlotSize - (barPadding * 2);
        var progressBarBounds = new Rectangle(0, 0, barWidth, barHeight);
        
        for (var i = 0; i < MaxSlots; i++)
        {
            var row = i / columns;
            var col = i % columns;

            var centerPosition = new Vector2(
                startPosition.X + col * (SlotSize + Gap) + SlotSize / 2f,
                startPosition.Y + row * (SlotSize + Gap) + SlotSize / 2f
            );

            CreateSlotEntity(centerPosition, slotBounds, progressBarBounds, i, onSlotClicked);
        }
    }

    private void CreateSlotEntity(Vector2 centerPosition, Rectangle slotBounds, Rectangle progressBarBounds, int index, Action<int> onSlotClicked)
    {
        // Background box button
        var slotBoxEntity = uiWorld.Create();
        uiWorld.Add(slotBoxEntity,
            new PositionComponent(centerPosition),
            new SpriteComponent(mPixelTexture, slotBounds, 0.7f) { mColor = sBackgroundBoxColor },
            new ButtonComponent(slotBounds, () =>
            {
                mAudioService.PlaySfx("Sounds/UI/AssignmentSound");
                
                onSlotClicked(index);
            }),
            new IsVisibleComponent(),
            new IgnoreCullingComponent()
        );

        // Icon entity
        var iconEntity = uiWorld.Create();
        uiWorld.Add(iconEntity,
            new PositionComponent(centerPosition),
            new SpriteComponent(null, Rectangle.Empty, 0.81f),
            new IsVisibleComponent(),
            new IgnoreCullingComponent()
        );

        // Text entity
        var textPosition = new Vector2(
            centerPosition.X + SlotSize / 2f - 4,
            centerPosition.Y + SlotSize / 2f - 8
        );

        var textEntity = uiWorld.Create();
        uiWorld.Add(textEntity,
            new PositionComponent(textPosition),
            new TextComponent("", mFont, Color.White, TextAlignment.MiddleRight, 0.95f)
        );
        
        var barPosition = new Vector2(
            centerPosition.X - (SlotSize / 2f) + 4, 
            centerPosition.Y + (SlotSize / 2f) - 6 - 4
        );

        var barEntity = uiWorld.Create();
        uiWorld.Add(barEntity,
            new PositionComponent(barPosition),
            new ProgressBarComponent 
            { 
                BackgroundBounds = progressBarBounds,
                ForegroundColor = Color.LightGreen,
                Max = 100f,
                Current = 0f,
                IsActive = false
            },
            new IgnoreCullingComponent()
        );
        
        mUiSlotEntities.Add((iconEntity, textEntity, barEntity));
    }

    /// <summary>
    /// Synchronizes the UI world with the game world and handles the tooltip logic
    /// Has to be called by State.Update
    /// </summary>
    public void Update(World gameWorld, Vector2 mousePosition)
    {
        UpdateInventorySync(gameWorld);
        HandleTooltipLogic(gameWorld, mousePosition);
    }
    
    /// <summary>
    /// Synchronizes the UI world with the game world
    /// Has to be called by State.Update
    /// </summary>
    private void UpdateInventorySync(World gameWorld)
    {
        // Get the player inventory
        InventoryComponent playerInventory = default;
        var bloodlustUnlocked = false;
        var specialAbility = ItemType.None;
        var query = new QueryDescription().WithAll<PlayerTagComponent, InventoryComponent, BloodlustTrackerComponent, MutantTypeComponent>();

        gameWorld.Query(in query, (ref InventoryComponent inv, ref BloodlustTrackerComponent bloodlust, ref MutantTypeComponent mutant) =>
        {
            playerInventory = inv;
            bloodlustUnlocked = bloodlust.IsUnlocked;
            specialAbility = PlayerDefinitions.Get(mutant.Type).SpecialItem;
        });

        if (playerInventory.mSlots == null) return;

        // Synchronize UI
        for (var i = 0; i < MaxSlots; i++)
        {
            if (i >= mUiSlotEntities.Count) continue;

            var (iconEntity, textEntity, barEntity) = mUiSlotEntities[i];
            var itemSlotEntity = playerInventory.mSlots[i];

            if (itemSlotEntity != Entity.Null && gameWorld.IsAlive(itemSlotEntity))
            {
                UpdateSlotWithItem(gameWorld, itemSlotEntity, iconEntity, textEntity, barEntity, specialAbility, bloodlustUnlocked);
            }
            else
            {
                ClearSlot(iconEntity, textEntity, barEntity);
            }
        }
    }

    private void UpdateSlotWithItem(World gameWorld, Entity itemSlotEntity, Entity iconEntity, Entity textEntity, Entity barEntity, ItemType specialAbility, bool bloodlustUnlocked)
    {
        ref var uiSprite = ref uiWorld.Get<SpriteComponent>(iconEntity);
        ref var uiText = ref uiWorld.Get<TextComponent>(textEntity);
        ref var uiBar = ref uiWorld.Get<ProgressBarComponent>(barEntity);

        ref var id = ref gameWorld.Get<ItemIdentificationComponent>(itemSlotEntity);
        var icon = services.ItemAssets.GetIcon(id.mType);

        if (icon != null)
        {
            uiSprite.SpriteSheet = icon;
            uiSprite.SourceRect = icon.Bounds;
            uiSprite.Origin = new Vector2(icon.Width / 2f, icon.Height / 2f);
            var scale = 0.9f * SlotSize / Math.Max(icon.Width, icon.Height);
            uiSprite.mScale = scale;

            uiSprite.mColor = (id.mType == specialAbility && !bloodlustUnlocked) ? Color.Black * 0.5f : Color.White;
        }

        if (gameWorld.Has<ItemStackComponent>(itemSlotEntity))
        {
            var count = gameWorld.Get<ItemStackComponent>(itemSlotEntity).mCount;
            uiText.Text = count > 0 ? count.ToString() : "";
        }
        else
        {
            uiText.Text = "";
        }

        if (gameWorld.Has<LifeTimeComponent>(itemSlotEntity))
        {
            var lifetime = gameWorld.Get<LifeTimeComponent>(itemSlotEntity);
            
            uiBar.IsActive = true;
            uiBar.Max = (float)lifetime.InitialLifeTimeSeconds;
            uiBar.Current = (float)lifetime.RemainingLifeTimeSeconds;
            var ratio = uiBar.Max > 0 ? uiBar.Current / uiBar.Max : 0;
            uiBar.ForegroundColor = ratio switch
            {
                > 0.5f => Color.LightGreen,
                > 0.2f => Color.Orange,
                _ => Color.Red
            };
            
            if (!uiWorld.Has<IsVisibleComponent>(barEntity)) uiWorld.Add(barEntity, new IsVisibleComponent());
        }
        else
        {
            uiBar.IsActive = false;
            if (uiWorld.Has<IsVisibleComponent>(barEntity)) uiWorld.Remove<IsVisibleComponent>(barEntity);
        }
    }

    private void ClearSlot(Entity iconEntity, Entity textEntity, Entity barEntity)
    {
        ref var uiSprite = ref uiWorld.Get<SpriteComponent>(iconEntity);
        ref var uiText = ref uiWorld.Get<TextComponent>(textEntity);
        ref var uiBar = ref uiWorld.Get<ProgressBarComponent>(barEntity);

        uiSprite.SpriteSheet = null;
        uiText.Text = "";
        uiBar.IsActive = false;
        
        if (uiWorld.Has<IsVisibleComponent>(barEntity)) uiWorld.Remove<IsVisibleComponent>(barEntity);
    }
    
    private void HandleTooltipLogic(World gameWorld, Vector2 mousePos)
    {
        var currentHoverIndex = -1;

        // Find the hovered slot
        for (var i = 0; i < mUiSlotEntities.Count; i++)
        {
            var (iconEntity, _, _) = mUiSlotEntities[i];
            if (!uiWorld.Has<PositionComponent>(iconEntity)) continue;

            ref var pos = ref uiWorld.Get<PositionComponent>(iconEntity);
            
            // AABB Check
            const float halfSize = SlotSize / 2f;
            if (mousePos.X < pos.Value.X - halfSize || mousePos.X > pos.Value.X + halfSize ||
                mousePos.Y < pos.Value.Y - halfSize || mousePos.Y > pos.Value.Y + halfSize) continue;

            currentHoverIndex = i;
            break;
        }

        // State Change Check
        if (currentHoverIndex == mHoveredSlotIndex) { return; }
        ClearTooltip();

        if (currentHoverIndex != -1)
        {
            var itemType = GetItemTypeAtIndex(gameWorld, currentHoverIndex);
            if (itemType != ItemType.None)
            {
                var def = ItemDefinitions.Get(itemType);
                mActiveTooltipEntities = mTooltipRenderer.CreateItemTooltip(uiWorld, def, mousePos);
            }
        }
            
        mHoveredSlotIndex = currentHoverIndex;
    }
    
    private void ClearTooltip()
    {
        foreach (var entity in mActiveTooltipEntities)
        {
            uiWorld.Destroy(entity);
        }
        mActiveTooltipEntities.Clear();
    }
    
    private static ItemType GetItemTypeAtIndex(World gameWorld, int index)
    {
        var foundType = ItemType.None;
        var query = new QueryDescription().WithAll<PlayerTagComponent, InventoryComponent>();
        gameWorld.Query(in query, (ref InventoryComponent inv) =>
        {
            if (inv.mSlots == null || index >= inv.mSlots.Length) return;

            var itemEntity = inv.mSlots[index];
            if (itemEntity != Entity.Null && gameWorld.IsAlive(itemEntity))
            {
                foundType = gameWorld.Get<ItemIdentificationComponent>(itemEntity).mType;
            }
        });
        return foundType;
    }

    public void Dispose()
    {
        mPixelTexture?.Dispose();
    }
}