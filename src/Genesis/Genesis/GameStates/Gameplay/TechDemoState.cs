using Arch.Core;
using Genesis.Architecture;
using Genesis.Architecture.Debug;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Components.Inventory;
using Genesis.Gameplay.Definitions;
using Genesis.Gameplay.Entities;
using Genesis.Gameplay.Extensions;
using Genesis.Gameplay.Navigation;
using Genesis.Gameplay.Systems;
using Genesis.Gameplay.Systems.Render;
using Genesis.GameStates.Overlays;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace Genesis.GameStates.Gameplay;

/// <summary>
/// Tech Demo State for testing purposes with a larger map and more entities.
/// Loads the map_techdemo.tmx map and allows for stress testing with 1000+ entities.
/// </summary>
public class TechDemoState : GameplayState
{
    private EnemyFactory mEnemySpawner;
    private CompanionFactory mCompanionFactory;
    private EffectFactory mEffectFactory;

    public override void Enter()
    {
        base.Enter();
        mSound.PlayMusic("Sounds/Music/Level 1");

        var enemySystem = mServices.Systems.Get<EnemyControlSystem>();
        var companionSystem = mServices.Systems.Get<CompanionControlSystem>();
        var debugRender = mServices.Systems.Get<DebugRenderSystem>();
        

        // Set this to true, to signal that we are now in the TechDemo and want to use the FlowField.
        if (enemySystem != null) { enemySystem.IsTechDemoMode = true; }
        if (companionSystem != null) { companionSystem.IsTechDemoMode = true; }
        mDebugOverlay.IsInsideTechDemo = true;

        // Initialize the FlowField shared between AI logic and Debug rendering.
        var flowField = mServices.World.GetResource<FlowField>();
        debugRender?.SetFlowField(flowField);

        // Spawn Entities
        mEnemySpawner = new EnemyFactory(mServices.Content);
        mCompanionFactory = new CompanionFactory(mServices.Content);
        mEffectFactory = new EffectFactory(mServices.Content);
        
        SpawnInitialEntities();
        GivePlayerDebugItems();
        CreateKeyTooltips();
    }

    public override void Exit()
    {
        base.Exit();
        
        var enemySystem = mServices.Systems.Get<EnemyControlSystem>();
        var companionSystem = mServices.Systems.Get<CompanionControlSystem>();

        if (enemySystem != null) { enemySystem.IsTechDemoMode = false; }
        if (companionSystem != null) { companionSystem.IsTechDemoMode = false; }

        mDebugOverlay.IsInsideTechDemo = false;
        mDebugOverlay.ShowFlowField = false;
    }

    public override void Resume()
    {
        base.Resume();
        mSound.PlayMusic("Sounds/Music/Level 1");
    }

    public override void HandleInput(InputService input)
    {
        base.HandleInput(input);

        // Input only needed for Techdemo
        // Spawn Companions when right Mouse Button is pressed and no companion command was made
        if (!input.IsRightMousePressed()) { return; }

        var worldPos = mCameraService.ScreenToWorld(input.GetMousePosition());

        var inputHandled = CompanionControlSystem.HandleCompanionCommand(mServices.World, worldPos);
        if (inputHandled) { return; }

        SpawnCompanionSwarm(worldPos);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        KeepPlayerStatsMaxed(mServices.World);
        UpdateCameraFollow();
    }

    private void SpawnInitialEntities()
    {
        // Spawn CEO
        mEnemySpawner.Create(mServices.World, new Vector2(1000, 1000), EnemyType.Ceo);

        // Mutant 1 (Laser Arm)
        mEnemySpawner.Create(mServices.World, new Vector2(1200, 1000), EnemyType.Mutant1);

        // Mutant 2 (Acid Spitter)
        mEnemySpawner.Create(mServices.World, new Vector2(1400, 1000), EnemyType.Mutant2);

        // Mutant 3 (Arms of Steel)
        mEnemySpawner.Create(mServices.World, new Vector2(1600, 1000), EnemyType.Mutant3);
    }

    private void GivePlayerDebugItems()
    {
        mServices.World.Create(new AddItemRequestComponent(ItemType.AcidSpit, 1));
        mServices.World.Create(new AddItemRequestComponent(ItemType.ArmsOfSteel, 1));
        mServices.World.Create(new AddItemRequestComponent(ItemType.Pistol, 1));
        mServices.World.Create(new AddItemRequestComponent(ItemType.HealthSyringe, 100));
        mServices.World.Create(new AddItemRequestComponent(ItemType.StunGrenade, 100));
        mServices.World.Create(new AddItemRequestComponent(ItemType.Neurochip, 100));
        mServices.World.Create(new AddItemRequestComponent(ItemType.Fist, 1));
        mServices.World.Create(new AddItemRequestComponent(ItemType.RemoteExplosive, 100));
    }

    private void CreateKeyTooltips()
    {
        const float lineSpacing = 20f;
        const int screenWidth = ScreenService.VirtualWidth;

        // Place the tooltips at the top-right under the run timer
        var startPos = new Vector2(screenWidth - 20, 80);

        string[] tooltips = {
        "[F12] Debugger",
        "[F11] FPS / Stats (Enemies & Companions)",
        "[RMB] Spawn Companion"
        };

        var uiFont = mServices.Content.Load<SpriteFont>("Fonts/HudFont");

        for (var i = 0; i < tooltips.Length; i++)
        {
            var position = new Vector2(startPos.X, startPos.Y + (i * lineSpacing));

            var textEntity = mServices.UiFactory.CreateText(
            mHudWorld.EcsWorld,
            position,
            tooltips[i],
            uiFont,
            Color.Black * 0.8f,
            TextAlignment.TopRight
            );

        // Make sure that the Ui elements are'nt removed by the culling system.
            mServices.UiFactory.MarkAsStaticUi(mHudWorld.EcsWorld, textEntity);
        }
    }

    private void SpawnCompanionSwarm(Vector2 worldPos)
    {
        var type = EnemyType.Mutant2;
        for (var x = 0; x < 250; x += 50)
        {
            for (var y = 0; y < 500; y += 50)
            {
                var gridPos = mGridMap.WorldToGrid(worldPos + new Vector2(x, y));
                if (mGridMap.IsWalkable(gridPos.X, gridPos.Y))
                {

                    var entity = mCompanionFactory.Create(mServices.World, worldPos + new Vector2(x, y), type);
                    mServices.World.RemoveIfExists<EnemyComponent>(entity);

                    if (type == EnemyType.Mutant2)
                    {
                        type = EnemyType.Mutant3;
                    }
                    else type = EnemyType.Mutant2;

                }
            }
        }
    }
    
    private void UpdateCameraFollow()
    {
        var playerQuery = new QueryDescription().WithAll<PlayerTagComponent, PositionComponent>();
        mServices.World.Query(in playerQuery, (ref PositionComponent pos) =>
        {
            mCameraService.ActiveCamera.LookAt(pos.Value);
        });
    }
}