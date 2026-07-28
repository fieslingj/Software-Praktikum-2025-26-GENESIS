namespace Genesis.Architecture;

public interface ICameraUser
{
    /// <summary>
    /// Inject the camera service into a state that requires it.
    /// </summary>
    /// <param name="cameraService"></param>
    void SetCameraService(CameraService cameraService);
}