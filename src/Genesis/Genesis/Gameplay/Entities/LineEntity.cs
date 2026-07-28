using Arch.Core;
using Genesis.Gameplay.Components;
using Microsoft.Xna.Framework;
namespace Genesis.Gameplay.Entities;
public static class LineEntity
{
    public static void Create(World world,Vector2 startPoint, Vector2 endPoint, float thickness,Color color)
    {
        world.Create(new LinePositionComponent(startPoint, endPoint, thickness,color));
    }
}