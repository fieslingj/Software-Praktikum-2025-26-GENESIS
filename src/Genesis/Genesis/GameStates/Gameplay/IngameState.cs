using Genesis.Architecture;
using Genesis.Architecture.Audio;
using Genesis.Gameplay.Components.World;
using Genesis.Gameplay.Extensions;
using Genesis.Gameplay.Systems;

namespace Genesis.GameStates.Gameplay;

public class IngameState : GameplayState
{
    public override void Initialize(GameStateManager manager, GameServices services, ScreenService screen, AudioService sound)
    {
        base.Initialize(manager, services, screen, sound);
        mLocalCamera.ZoomOut(0.1f);
    }

    public override void Enter()
    {
        base.Enter();
        PlayLevelMusic();
    }

    public override void Resume()
    {
        base.Resume();
        PlayLevelMusic();
    }

    public override void HandleInput(InputService input)
    {
        base.HandleInput(input);

        // Handle companion commands here, since the tech-demo has two right click actions
        if (input.IsRightMousePressed())
        {
            var mousePos = mCameraService.ScreenToWorld(input.GetMousePosition());
            CompanionControlSystem.HandleCompanionCommand(mServices.World, mousePos);
        }
    }

    private void PlayLevelMusic()
    {
        var floorLayout = mServices.World.GetResource<FloorLayoutComponent>();
        if (floorLayout == null) return;

        var songName = floorLayout.Layer switch
        {
            2 => "Sounds/Music/Level 2",
            3 => "Sounds/Music/Level 3",
            _ => "Sounds/Music/Level 1"
        };

        mSound.PlayMusic(songName);
    }
}