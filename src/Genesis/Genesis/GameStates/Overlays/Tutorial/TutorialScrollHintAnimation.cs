using Microsoft.Xna.Framework;

namespace Genesis.GameStates.Overlays.Tutorial;

/// <summary>
/// Controls the blinking animation for the scroll hint in the tutorial overlay.
/// </summary>
public class TutorialScrollHintAnimation
{
    private double mTimer;
    private bool mIsVisible = true;

    public bool IsVisible => mIsVisible;

    public void Update(GameTime gameTime)
    {
        mTimer += gameTime.ElapsedGameTime.TotalSeconds;

        if (mTimer >= TutorialLayoutSettings.ScrollGlyphBlinkInterval)
        {
            mTimer -= TutorialLayoutSettings.ScrollGlyphBlinkInterval;
            mIsVisible = !mIsVisible;
        }
    }

    public void Reset()
    {
        mTimer = 0;
        mIsVisible = true;
    }
}