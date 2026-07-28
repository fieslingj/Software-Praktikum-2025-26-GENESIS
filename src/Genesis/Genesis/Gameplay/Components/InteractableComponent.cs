namespace Genesis.Gameplay.Components;

public struct InteractableComponent(float radius, InteractionType type)
{
    public float Radius { get; } = radius;
    public InteractionType Type { get; } = type;
    public bool LightOn { get; set; } = false;
}

public enum InteractionType { Generic, Trap, SnackMachine, Corpse, Door, ChemicalTank, Table, Elevator }