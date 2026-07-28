using Arch.Core;
using Genesis.Architecture;
using Genesis.Architecture.ECS;
using Genesis.Gameplay.Components;
using Microsoft.Xna.Framework;

namespace Genesis.Gameplay.Systems;

public class PlayerFacingSystem(CameraService cameraService) : IInputSystem
{
    private static readonly QueryDescription sPlayerQuery = new QueryDescription()
        .WithAll<PlayerTagComponent,FaceComponent>();
    
    public void HandleInput(World world, InputService input)
    {
        // Transform the mouse position into the world position
        var mousePositionRaw = input.GetMousePosition();
        var mousePositionWorld = cameraService.ScreenToWorld(mousePositionRaw);

        world.Query(in sPlayerQuery, (ref FaceComponent face, ref PositionComponent pos) =>
        {
            face.FaceDirection = FaceAngle(mousePositionWorld - pos.Value);
        });
    }
    
    /// <summary>Maps the Vector2 angle to one of four directions.</summary>
    public static FaceDirection FaceAngle(Vector2 faceDirection)
    {
        var angle = double.Atan2(faceDirection.Y,faceDirection.X);
        return angle switch
        {
            <  0.25 * double.Pi and > -0.25 * double.Pi => FaceDirection.East,
            < -0.25 * double.Pi and > -0.75 * double.Pi => FaceDirection.North,
            >  0.75 * double.Pi or  < -0.75 * double.Pi => FaceDirection.West,
            _ => FaceDirection.South
        };
    }
}
