using System.Collections.Generic;

namespace Genesis.Gameplay.Definitions;
/// <summary>
/// Statuseffect of Entity unrelated to State
/// </summary>
public enum StatusType
{
    None = 0,
    AcidSour,
    InAcid,
    Stunned
}

public sealed class StatusDefinition
{
    public int DamagePerSecond { get; init; }
    public float TimeOfEffect { get; init; }
}
public static class StatusTypeDefinitions
{
    private static readonly StatusDefinition sDefault = new();
    
    private static readonly Dictionary<StatusType, StatusDefinition> sDefinitions = new()
    {
        {StatusType.AcidSour, new StatusDefinition { DamagePerSecond = 10 , TimeOfEffect = 3f}},
        {StatusType.InAcid, new StatusDefinition { DamagePerSecond = 10 , TimeOfEffect = 0.05f}},
        {StatusType.Stunned, new StatusDefinition { TimeOfEffect = 5 }}
    };
    
    public static StatusDefinition Get(StatusType type) => sDefinitions.GetValueOrDefault(type, sDefault);
}