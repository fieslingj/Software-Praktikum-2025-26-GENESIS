using Microsoft.Xna.Framework;

namespace Genesis.Gameplay.Components;

public struct CoverBehaviorComponent(float searchRadius, float coverPreference)
{
    public float SearchRadius { get; } = searchRadius;
    public float CoverPreference { get; } = coverPreference;
    
    // Runtime Status
    public bool IsTakingCover { get; set; } = false;
    public Vector2? CurrentCoverPos { get; set; } = null;
    public float CoverCooldown { get; set; } = 0;
}