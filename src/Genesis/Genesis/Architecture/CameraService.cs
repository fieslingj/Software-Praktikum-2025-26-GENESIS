using Microsoft.Xna.Framework;
using MonoGame.Extended;

namespace Genesis.Architecture;

public class CameraService
{
    // The currently active camera. Can be null e.g. in MainMenu
    public OrthographicCamera ActiveCamera { get; set; }
    
    // Shake parameters applied by ScreenShakingSystem
    public Vector2 ShakeOffset { get; set; }
    public float ShakeRotation { get; set; }

    /// <summary>
    /// Converts Mouse Coordinates (Screen) to Game Coordinates (World)
    /// </summary>
    public Vector2 ScreenToWorld(Vector2 screenPosition)
    {
        return ActiveCamera?.ScreenToWorld(screenPosition) ?? screenPosition;
    }

    /// <summary>
    /// Converts Mouse Coordinates (Screen) to Game Coordinates (World)
    /// </summary>
    public Vector2 ScreenToWorld(Point screenPosition)
    {
        var screenPosVector = screenPosition.ToVector2();
        return ActiveCamera?.ScreenToWorld(screenPosVector) ?? screenPosVector;
    }

    /// <summary>
    /// Gets the View Matrix for SpriteBatch, including shake effects.
    /// </summary>
    public Matrix GetViewMatrix()
    {
        if (ActiveCamera == null) return Matrix.Identity;

        // Apply shake temporarily to the camera's view matrix
        // We do this by creating a transformation matrix for the shake
        // and multiplying it with the camera's view matrix.
        // Or simpler: We can manually construct the matrix if we don't want to modify the camera state.
        var cameraMatrix = ActiveCamera.GetViewMatrix();
        
        // Create shake translation and rotation matrix
        var shakeMatrix = Matrix.CreateTranslation(new Vector3(ShakeOffset, 0)) * 
                          Matrix.CreateRotationZ(ShakeRotation);
                          
        // If we want rotation to be around the center, we might need more complex matrix math,
        // but for small shakes, this is often enough, or we apply it before the camera matrix.
        return cameraMatrix * shakeMatrix;
    }
}