namespace Genesis.Gameplay.Components;

/// <summary>
/// Represents the current state of any (living) Entity
/// </summary>
public enum ActorState
{
    Idle,
    Walking,
    Sprinting,
    Attacking,
    Hit,
    Dead,
    Stunned,
    Ducking
}

/// <summary>
/// Holds the current and previous state of any entity.
/// Required by the systems <see cref="Systems.AnimationSystem"/> and <see cref="Systems.MovementSoundSystem"/>
/// to detect changes in the behavioral states (e.g. Idle to Walking).
/// </summary>
public struct StateComponent
{
    /// <summary>
    ///  The state this entity is in this frame.
    /// </summary>
    public ActorState Current { get; set; }
    /// <summary>
    /// The state this entity was in the last frame.
    /// </summary>
    public ActorState Previous { get; set; }
    
    /// <summary>
    /// Time when CurrentState changed
    /// </summary>
    public double PersistenceTime { get; set; }
}