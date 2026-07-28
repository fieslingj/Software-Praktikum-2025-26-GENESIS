namespace Genesis.Gameplay.Components;

public enum TankState
{
    Intact,
    Destroyed
}

public class ChemicalTankComponent(TankState state)
{
    public TankState State { get; set; } = state;
    public bool PuddleCreated { get; set; } = false;
}

    