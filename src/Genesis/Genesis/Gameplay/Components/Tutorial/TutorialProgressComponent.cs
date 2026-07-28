using System;
using Microsoft.Xna.Framework;

namespace Genesis.Gameplay.Components.Tutorial;

/// <summary>
/// Saves the progress of shown tutorial hints.
/// </summary>
[Serializable]
public struct TutorialProgressComponent
{ 
    public bool MovementShown { get; set; }
    public bool AttackingShown { get; set; }
    public bool DoorInteractionShown { get; set; }
    public bool SnackMachineShown { get; set; }
    public bool BearTrapInteractionShown { get; set; }
    public bool ChemicalTankInteractionShown { get; set; }
    public bool TableInteractionShown { get; set; }
    public bool CorpseInteractionShown { get; set; }
    public bool MutantRoomShown { get; set; }
    
    /// <summary>
    /// Saves the position of the last room where a tutorial hint was shown.
    /// </summary>
    public Vector2 LastRoomPosition { get; set; }
}