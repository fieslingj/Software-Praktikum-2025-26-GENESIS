using System;
using Arch.Core;
using Genesis.Architecture;
using Genesis.Architecture.Audio;
using Genesis.Architecture.Debug;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Components.Purchase;
using Genesis.Gameplay.Extensions;
using Genesis.Gameplay.Navigation;
using Genesis.Gameplay.Systems;
using Genesis.GameStates.Overlays;
using Genesis.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace Genesis.GameStates.Gameplay;

public abstract class GameplayState : IGameState, ICameraUser, IHudController
{
    protected GameServices mServices;
    protected GameStateManager mStateManager;
    protected ScreenService mScreenService;
    protected CameraService mCameraService;
    protected OrthographicCamera mLocalCamera;
    protected AudioService mSound;

    protected GridMap mGridMap;
    protected HudWorld mHudWorld;
    protected DebugOverlay mDebugOverlay;

    public virtual void Initialize(GameStateManager manager, GameServices services, ScreenService screen, AudioService sound)
    {
        mServices = services;
        mStateManager = manager;
        mScreenService = screen;
        mSound = sound;
        mLocalCamera = new OrthographicCamera(mScreenService.Adapter);
        mHudWorld = new HudWorld(mServices, mScreenService, mStateManager);
        mDebugOverlay = services.Systems.Get<DrawSystem>().DebugOverlay;

        // GridMap should be loaded by MapLoader task before this state
        mGridMap = mServices.World.GetResource<GridMap>();
        if (mGridMap == null)
        {
            throw new InvalidOperationException("GridMap was not initialized by MapLoader!");
        }
        
        // Center camera on map
        var roomEntity = services.World.GetFirstEntity(new QueryDescription().WithExclusive<TiledMapComponent>());
        if (roomEntity != Entity.Null)
        {
            var map = services.World.Get<TiledMapComponent>(roomEntity).Map;
            var center = new Vector2(map.WidthInPixels, map.HeightInPixels) / 2;
            mLocalCamera.LookAt(center);
        }
    }

    public void SetCameraService(CameraService cameraService)
    {
        mCameraService = cameraService;
    }

    public virtual void Enter()
    {
        mCameraService.ActiveCamera = mLocalCamera;
        SetSystems();
    }

    public virtual void Exit()
    {
        StopAllMovementSounds();
        mHudWorld.Dispose();
        CloseDebugOverlay();
    }

    public virtual void Pause()
    {
        var query = new QueryDescription().WithAll<MovementSoundComponent>();
        mServices.World.Query(in query, (ref MovementSoundComponent movementSound) =>
        {
            if (movementSound.WalkSoundInstance?.State == SoundState.Playing) movementSound.WalkSoundInstance.Pause();
            if (movementSound.SprintSoundInstance?.State == SoundState.Playing) movementSound.SprintSoundInstance.Pause();
        });
    }

    public virtual void Resume()
    {
        ResumeMovementSounds();
        mHudWorld.SetInventoryOpen(false);
        mServices.InputService.pausetime = 250;
    }

    public virtual void HandleInput(InputService input)
    {
        if (input.IsActionPressed(InputAction.Pause)) mStateManager.PushState(new PauseMenuState());
        if (input.IsActionPressed(InputAction.OpenInventory)) OpenInventory();

        mServices.Systems.HandleInput(mServices.World, input);
        HandleHudInput(input);
    }

    public void HandleHudInput(InputService input)
    {
        mServices.Systems.Get<ButtonInputSystem>().HandleInput(mHudWorld.EcsWorld, input);
    }

    public virtual void Update(GameTime gameTime)
    {
        if (IsPlayerDead())
        {
            mStateManager.PushState(new GameOverState());
            return;
        }

        mServices.Systems.Update(mServices.World, gameTime);
        UpdateHud(gameTime);
    }

    public void UpdateHud(GameTime gameTime)
    {
        mHudWorld.Update(mServices.World, gameTime);
    }

    public virtual void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var viewMatrix = mCameraService.GetViewMatrix();
        var uiScale = mScreenService.GetUiScale();

        spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: viewMatrix, sortMode: SpriteSortMode.FrontToBack);
        mServices.Systems.Draw(mServices.World, spriteBatch, true);
        spriteBatch.End();

        spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: Matrix.CreateScale(uiScale, uiScale, 1.0f), sortMode: SpriteSortMode.FrontToBack);
        mHudWorld.Draw(spriteBatch, mServices.Systems.Get<DrawSystem>());
        spriteBatch.End();
    }

    protected void SetSystems()
    {
        mServices.Systems.ToggleSystem(typeof(EnemyControlSystem), true);
        mServices.Systems.ToggleSystem(typeof(StaminaSystem), true);
        mServices.Systems.ToggleSystem(typeof(MovementSystem), true);
        mServices.Systems.ToggleSystem(typeof(EntityCollisionSystem), true);
        mServices.Systems.ToggleSystem(typeof(MovementSoundSystem), true);
        mServices.Systems.ToggleSystem(typeof(AnimationSystem), true);
        mServices.Systems.ToggleSystem(typeof(TrapSystem), true);
        mServices.Systems.ToggleSystem(typeof(DamageSystem), true);
        mServices.Systems.ToggleSystem(typeof(DeathSystem), true);
        mServices.Systems.ToggleSystem(typeof(AttackSystem), true);
        mServices.Systems.ToggleSystem(typeof(LifeTimeSystem), true);
        mServices.Systems.ToggleSystem(typeof(ProximityLightSystem), true);
        mServices.Systems.ToggleSystem(typeof(InventorySystem), true);
        mServices.Systems.ToggleSystem(typeof(ChemicalTankSystem), true);
        mServices.Systems.ToggleSystem(typeof(StatuseffectSystem), true);
        mServices.Systems.ToggleSystem(typeof(TableSystem), true);
    }


    protected void ResumeMovementSounds()
    {
        var query = new QueryDescription().WithAll<MovementSoundComponent>();
        mServices.World.Query(in query, (ref MovementSoundComponent movementSound) =>
        {
            if (movementSound.WalkSoundInstance?.State == SoundState.Paused) movementSound.WalkSoundInstance.Resume();
            if (movementSound.SprintSoundInstance?.State == SoundState.Paused) movementSound.SprintSoundInstance.Resume();
        });
    }

    protected void StopAllMovementSounds()
    {
        var query = new QueryDescription().WithAll<MovementSoundComponent>();
        mServices.World.Query(in query, (ref MovementSoundComponent sound) =>
        {
            if (sound.WalkSoundInstance != null) { mSound.StopSfxInstance(sound.WalkSoundInstance); sound.WalkSoundInstance = null; }
            if (sound.SprintSoundInstance != null) { mSound.StopSfxInstance(sound.SprintSoundInstance); sound.SprintSoundInstance = null; }
        });
    }

    protected void OpenInventory()
    {
        mStateManager.PushState(new InventoryState());
        mHudWorld.SetInventoryOpen(true);
    }

    protected bool IsPlayerDead()
    {
        var deadPlayerQuery = new QueryDescription().WithAll<PlayerTagComponent, DeathStateComponent>();
        return mServices.World.CountEntities(in deadPlayerQuery) > 0;
    }

    protected void KeepPlayerStatsMaxed(World world)
    {
        var player = world.GetFirstEntity(new QueryDescription().WithAny<PlayerTagComponent>());
        if (player == Entity.Null) return;

        ref var hp = ref world.Get<HealthComponent>(player);
        hp.Current = float.MaxValue;
        hp.Max = float.MaxValue;
        
        ref var stamina = ref world.Get<StaminaComponent>(player);
        stamina.Current = float.MaxValue;
        stamina.Max = float.MaxValue;
        
        ref var ammo = ref world.Get<AmmoComponent>(player);
        ammo.Current = int.MaxValue;
        
        ref var coin = ref world.Get<CoinsComponent>(player);
        coin.CurrentAmount = int.MaxValue;
    }

    protected void CloseDebugOverlay()
    {
        if (mDebugOverlay.DebugEnabled)
        {
            mDebugOverlay.ToggleDebug = true;
        }
        if (mDebugOverlay.DebugCounterEnabled)
        {
            mDebugOverlay.ToggleDebugCounter = true;
        }
    }
}