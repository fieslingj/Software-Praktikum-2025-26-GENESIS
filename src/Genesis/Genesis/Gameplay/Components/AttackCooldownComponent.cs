namespace Genesis.Gameplay.Components;

public struct AttackCooldownComponent(float delay)
{
    public float Delay { get; set; } = delay;
    public float CurrentTime { get; set; } = 0;
}