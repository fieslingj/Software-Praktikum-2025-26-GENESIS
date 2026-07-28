using Microsoft.Xna.Framework.Audio;

namespace Genesis.Gameplay.Components;

/// <summary>
/// Holds the playable <see cref="SoundEffectInstance"/> objects for an entity's movement.
/// Each entity that makes sounds, will get it own instance of this component.
/// </summary>

public struct MovementSoundComponent
{
    // The playable, looping sound instance for the player walking.
    // This is the 'player' that MovementSoundSystem will control
    // by the SoundEffectInstance methods .Play() and .Stop().
    public SoundEffectInstance WalkSoundInstance { get; set; }
    public SoundEffectInstance SprintSoundInstance { get; set; }
    
    // Paths for AudioService
    public string WalkSoundPath { get; init; }
    public string SprintSoundPath { get; init; }
}