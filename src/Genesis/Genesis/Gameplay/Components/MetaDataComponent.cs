using Genesis.Persistence.Meta;

namespace Genesis.Gameplay.Components;

/// <summary>
/// A singleton component that holds the persistent global data
/// (Statistics and Achievements) while the game is running.
/// </summary>
public readonly struct MetaDataComponent(MetaData data)
{
    public MetaData Data { get; } = data;
}