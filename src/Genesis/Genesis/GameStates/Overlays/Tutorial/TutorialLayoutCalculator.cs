using Genesis.Architecture;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Genesis.GameStates.Overlays.Tutorial;

public class TutorialLayoutCalculator
{
    private readonly Texture2D mPanelTexture;

    public TutorialLayoutCalculator(Texture2D panelTexture)
    {
        mPanelTexture = panelTexture;
    }

    public Vector2 CalculatePanelPosition(float screenCenterX, float screenCenterY)
    {
        return new Vector2(
            screenCenterX - (mPanelTexture.Width / 2f),
            screenCenterY - (mPanelTexture.Height / 2f));
    }

    public Vector2 CalculateHeaderPosition(Vector2 panelPosition, Vector2 headerSize)
    {
        return new Vector2(
            panelPosition.X + (mPanelTexture.Width - headerSize.X) * 0.5f,
            panelPosition.Y + ((float)ScreenService.VirtualHeight / TutorialLayoutSettings.HeaderOffsetY));
    }

    public Rectangle CalculateTextFieldBounds(Vector2 panelPosition)
    {
        var innerMargin = TutorialLayoutSettings.InnerMarginRatio;
        var textBoxOffsetY = mPanelTexture.Height * TutorialLayoutSettings.TextBoxOffsetYRatio;
        var textFieldWidth = mPanelTexture.Width - (innerMargin * 2f);
        var textFieldHeight = mPanelTexture.Height * TutorialLayoutSettings.TextBoxHeightRatio;

        return new Rectangle(
            (int)(panelPosition.X + innerMargin),
            (int)(panelPosition.Y + textBoxOffsetY),
            (int)textFieldWidth,
            (int)textFieldHeight);
    }

    public float CalculateMaxLineWidth()
    {
        var textFieldWidth = mPanelTexture.Width - (TutorialLayoutSettings.InnerMarginRatio * 2f);
        return (textFieldWidth - (TutorialLayoutSettings.TextBoxPaddingX * 2f)) / TutorialLayoutSettings.BodyTextScale;
    }

    public Vector2 CalculateTextPosition(Rectangle textFieldBounds)
    {
        return new Vector2(
            textFieldBounds.X + TutorialLayoutSettings.TextBoxPaddingX,
            textFieldBounds.Y + TutorialLayoutSettings.TextBoxPaddingY);
    }

    public Vector2 CalculateButtonSize()
    {
        return new Vector2(
            ScreenService.VirtualWidth * TutorialLayoutSettings.ButtonWidthRatio,
            ScreenService.VirtualWidth * TutorialLayoutSettings.ButtonHeightRatio);
    }

    public float CalculateButtonY()
    {
        return ScreenService.VirtualHeight - TutorialLayoutSettings.ButtonYOffsetFromBottom;
    }
}