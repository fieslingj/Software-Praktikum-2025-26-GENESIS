using MonoGame.Extended;

namespace Genesis.Gameplay.Components;

public readonly struct CameraComponent(OrthographicCamera camera)
{
    public OrthographicCamera Camera { get; } = camera;
}