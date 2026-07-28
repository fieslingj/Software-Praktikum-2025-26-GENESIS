using System;
using Microsoft.Xna.Framework;

namespace Genesis.GameStates.Overlays.Tutorial;

/// <summary>
/// Controls the typewriter effect for displaying tutorial text character by character.
/// </summary>
public class TutorialTypewriterEffect
{
    private string mTargetText = string.Empty;
    private string mVisibleText = string.Empty;
    private int mCurrentIndex;
    private double mTimer;
    private bool mIsComplete;

    public string VisibleText => mVisibleText;
    public bool IsComplete => mIsComplete;

    public void StartNewText(string text)
    {
        mTargetText = text ?? string.Empty;
        mVisibleText = string.Empty;
        mCurrentIndex = 0;
        mTimer = 0;
        mIsComplete = false;
    }

    public void Update(GameTime gameTime)
    {
        if (mIsComplete || mCurrentIndex >= mTargetText.Length)
        {
            mIsComplete = true;
            return;
        }

        mTimer += gameTime.ElapsedGameTime.TotalSeconds;

        if (mTimer >= TutorialLayoutSettings.TypewriterSpeed)
        {
            mCurrentIndex++;
            mVisibleText = mTargetText[..mCurrentIndex];
            mTimer = 0;

            if (mCurrentIndex >= mTargetText.Length)
            {
                mIsComplete = true;
            }
        }
    }

    public void Complete()
    {
        mVisibleText = mTargetText;
        mCurrentIndex = mTargetText.Length;
        mIsComplete = true;
    }

    public void Reset()
    {
        mTargetText = string.Empty;
        mVisibleText = string.Empty;
        mCurrentIndex = 0;
        mTimer = 0;
        mIsComplete = false;
    }
}