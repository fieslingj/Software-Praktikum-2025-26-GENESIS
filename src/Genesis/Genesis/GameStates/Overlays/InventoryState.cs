using Arch.Core;
using Genesis.Architecture;
using Genesis.Architecture.Audio;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Systems;
using Genesis.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Genesis.GameStates.Overlays;

public class InventoryState : IGameState
{
    private World mUiWorld;
    private GameStateManager mStateManager;
    private GameServices mServices;
    private ScreenService mScreenService;

    private Texture2D mPixelTexture;
    private InventoryUiController mInventoryUi;
    private IHudController mHudController;

    private Vector2 mCurrentMousePosition;

    private AudioService mAudioService;

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
        mPixelTexture = new Texture2D(mScreenService.Graphics, 1, 1);
        mPixelTexture.SetData([Color.White]);
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
        if (input.IsActionPressed(InputAction.OpenInventory)
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
        mServices.Systems.Get<InventorySystem>().Update(mServices.World, gameTime);
        mServices.Systems.Get<RunTimerSystem>().Update(mServices.World, gameTime);
        mInventoryUi.Update(mServices.World, mCurrentMousePosition);

        mHudController?.UpdateHud(gameTime);
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        mStateManager.DrawBelowTop(gameTime, spriteBatch);

        float uiScale = mScreenService.GetUiScale();
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
        // Create background entity.
        var bgEntity = mServices.UiFactory.MarkAsStaticUi(mUiWorld, mUiWorld.Create());

        mUiWorld.Add(bgEntity,
            new PositionComponent(new Vector2(ScreenService.VirtualWidth / 2f, ScreenService.VirtualHeight / 2f)),
            new SpriteComponent(
                spriteSheet: mPixelTexture,
                sourceRect: new Rectangle(0, 0,  3 * ScreenService.VirtualWidth / 4, 3 * ScreenService.VirtualHeight / 4),
                layerDepth: 0f,
                scale: 1.0f
            )
            {
                mColor = new Color(0, 0, 0, 200)
            });

        const int columns = 5;
        const float width = columns * InventoryUiController.SlotSize + (columns - 1) * InventoryUiController.Gap;

        const float startX = (ScreenService.VirtualWidth - width) / 2f;
        const float startY = ScreenService.VirtualHeight / 2f - 100;

        mInventoryUi.BuildUi(
            startPosition: new Vector2(startX, startY),
            columns: 5,
            onSlotClicked: (index) => InventorySystem.AssignItemToHotbar(mServices.World, index)
            );
    }
}