using Microsoft.Xna.Framework;

namespace Genesis.Gameplay.Components;

/// <summary>
/// Represents the data necessary to visually draw a progress bar, 
/// displaying a current value relative to a maximum value.
/// </summary>
public struct ProgressBarComponent
{
    // The bounding box of the background bar.
    public Rectangle BackgroundBounds { get; init; } 
    
    // The color of the foreground bar.
    public Color ForegroundColor { get; set; } 
    
    // The current value (e.g. CurrentHealth)
    public float Current { get; set; } 
    
    // Der maximale Wert (e.g. MaxHealth)
    public float Max { get; set; }
    public bool IsActive { get; set; }
}