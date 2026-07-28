namespace Genesis.Gameplay.Components;

public enum TableState
{
    Standing,
    Flipped
}

public class TableComponent(TableState state)
{
    public TableState State { get; set; } = state;
    public bool IsInteractedWith { get; set; } = false;
}