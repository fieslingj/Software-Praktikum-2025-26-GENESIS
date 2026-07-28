using Genesis.Architecture;
using Genesis.Architecture.ECS; 
using Arch.Core;
using Genesis.Architecture.Persistence;

namespace Genesis.Gameplay.Systems;
/// <summary>
/// Bei Drücken von F5 wird in savePath gespeichert.
/// </summary>
public class SaveSystem : IInputSystem
{
    public void HandleInput(World world, InputService input)
    {
        if (input.IsActionDown(InputAction.Save))
        {
            SaveManager.SaveRun(world, 0);
        }
    }
}