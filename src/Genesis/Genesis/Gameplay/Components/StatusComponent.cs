using System.Collections.Generic;
using Genesis.Gameplay.Definitions;

namespace Genesis.Gameplay.Components;


/// <summary>
/// Statuseffects of entity unabhängig vom Actorstate
/// </summary>
/// <param name="statusTypesList">List of tuple of statūs with time since creation in s</param>
public struct StatusComponent(List<(StatusType,double)> statusTypesList)
{
    public List<(StatusType, double)> Types { get; } = statusTypesList;
}