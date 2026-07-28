namespace Genesis.Gameplay.Components;

/// <summary>
/// Time till Entity is deleted when active
/// </summary>
public struct LifeTimeComponent(double lifeTimeSeconds, double initialLifeTimeSeconds, bool active = false)
{
    public double RemainingLifeTimeSeconds { get; set; } = lifeTimeSeconds;
    public double InitialLifeTimeSeconds { get; set; } = initialLifeTimeSeconds;
    public bool Active { get; set; } = active;

    public LifeTimeComponent(double lifeTimeSeconds, bool active = false)
        : this(lifeTimeSeconds, lifeTimeSeconds, active) {}
}