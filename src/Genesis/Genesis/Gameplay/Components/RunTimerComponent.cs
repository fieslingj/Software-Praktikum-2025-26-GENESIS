using System;

namespace Genesis.Gameplay.Components;

/// <summary>
/// Stores the accumulated time of the current run in seconds.
/// </summary>
[Serializable]
public struct RunTimerComponent()
{
    public double TotalSeconds { get; set; } = 0;
}