using Arch.Core;
using Genesis.Architecture.Audio;
using Genesis.Architecture.ECS;
using Genesis.Gameplay.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace Genesis.Gameplay.Systems;

/// <summary>
/// Manages the playback of the sound.
/// According to the current state of an entity
/// it controls the corresponding sound instance for the state and entity. 
/// </summary>
public class MovementSoundSystem(AudioService audioService) : IUpdateSystem
{
    private AudioService mAudioService = audioService;
    
    // Query for all entities that have both a State and a MovementSound component.
    // Reads the state and accesses the sound instance.
    private readonly QueryDescription mSQueryDesc = new QueryDescription()
        .WithAll<StateComponent, MovementSoundComponent>();

    /// <summary>
    ///  Update all movement sounds. Playing or stopping them based on state changes.
    /// This is called every frame by IngameState. 
    /// </summary>
    /// <remarks>
    /// This method intentionally reacts only to state transitions.
    /// This is more efficient than polling the state every frame,
    /// as it reduces unnecessary checks and ensures sound commands (.Play(), .Stop())
    /// are sent only ones per state change.
    /// </remarks>
    public void Update(World world, GameTime gameTime)
    {
        // Iterate over all entities matching the query.
        world.Query(in mSQueryDesc,
            (ref StateComponent state, ref MovementSoundComponent sound) =>
            {
                if (state.Current == state.Previous) {return;}
                
                HandleSoundState(state.Current, ref sound);
            });
    }

    private void HandleSoundState(ActorState currentState, ref MovementSoundComponent sound)
{
    // Check if the actor is walking
    if (currentState is ActorState.Walking)
    {
        // Ensure sprint sound is stopped when transitioning to walking
        StopSprint(ref sound);

        if (sound.WalkSoundInstance == null || 
            sound.WalkSoundInstance.State == SoundState.Stopped || 
            sound.WalkSoundInstance.IsDisposed)
        {
            // Start the looping walk sound
            sound.WalkSoundInstance = mAudioService.PlaySfxInstance(sound.WalkSoundPath, true);
        }
    }
    // Check if the actor is sprinting
    else if (currentState is ActorState.Sprinting)
    {
        // Ensure walk sound is stopped when transitioning to sprinting
        StopWalk(ref sound);

        if (sound.SprintSoundInstance == null || 
            sound.SprintSoundInstance.State == SoundState.Stopped || 
            sound.SprintSoundInstance.IsDisposed)
        {
            // Start the looping sprint sound
            sound.SprintSoundInstance = mAudioService.PlaySfxInstance(sound.SprintSoundPath, true);
        }
    }
    // Handle idle or other states where no movement sound should play
    else
    {
        StopWalk(ref sound);
        StopSprint(ref sound);
    }
}

/// <summary>
/// Stops the walk sound instance and clears the reference.
/// </summary>
private void StopWalk(ref MovementSoundComponent sound)
{
    if (sound.WalkSoundInstance != null && !sound.WalkSoundInstance.IsDisposed)
    {
        mAudioService.StopSfxInstance(sound.WalkSoundInstance);
    }
    sound.WalkSoundInstance = null;
}

/// <summary>
/// Stops the sprint sound instance and clears the reference.
/// </summary>
private void StopSprint(ref MovementSoundComponent sound)
{
    if (sound.SprintSoundInstance != null && !sound.SprintSoundInstance.IsDisposed)
    {
        mAudioService.StopSfxInstance(sound.SprintSoundInstance);
    }
    sound.SprintSoundInstance = null;
}
}