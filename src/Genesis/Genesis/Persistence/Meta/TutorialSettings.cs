using System;
using Microsoft.Xna.Framework;

namespace Genesis.Persistence.Meta;

/// <summary>
/// Stores tutorial-related settings and progress.
/// </summary>
[Serializable]
public class TutorialSettings
{
    /// <summary>
    /// Whether tutorials should be displayed.
    /// </summary>
    public bool TutorialEnabled { get; set; } = true;

    /// <summary>
    /// Progress of shown tutorial hints.
    /// </summary>
    public bool MovementShown { get; set; } = false;
    public bool AttackingShown { get; set; } = false;
    public bool DoorInteractionShown { get; set; } = false;
    public bool SnackMachineShown { get; set; } = false;
    public bool BearTrapInteractionShown { get; set; } = false;
    public bool ChemicalTankInteractionShown { get; set; } = false;
    public bool TableInteractionShown { get; set; } = false;
    public bool CorpseInteractionShown { get; set; } = false;
    public bool MutantRoomShown { get; set; } = false;
        
    /// <summary>
    /// Last room position where a tutorial was shown.
    /// </summary>
    public Vector2 LastRoomPosition { get; set; }
}