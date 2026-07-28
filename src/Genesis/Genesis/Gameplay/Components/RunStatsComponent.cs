
namespace Genesis.Gameplay.Components;

public enum RunType
{
    Normal = 0,
    Techdemo = 1,
}


public struct RunStatsComponent(RunType runtype = RunType.Normal)
{
    public int EnemiesDefeated { get; set; }
    public float DamageDealt { get; set; }
    public float DamageTaken { get; set; }
    public int RoomsExplored { get; set; }
    public int RunType { get; set; } = (int)runtype;
}
