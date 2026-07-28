namespace Genesis.Gameplay.Components;

public struct DeathStateComponent(float duration)
{
    public float DespawnTimer { get; } = duration;
    public float TimeSinceDeath { get; set; } = 0f;
}