namespace Genesis.Gameplay.Components;

public struct ShakeSourceComponent(float trauma, float decay, float maxOffset = 15f, float maxRotation = 0.1f, bool isContinuous = false)
{
    // State
    public float Trauma { get; set; } = trauma;

    // Configuration
    public float Decay { get; } = decay;
    public float MaxOffset { get; } = maxOffset;
    public float MaxRotation { get; } = maxRotation;
    public bool IsContinuous { get; } = isContinuous;
}