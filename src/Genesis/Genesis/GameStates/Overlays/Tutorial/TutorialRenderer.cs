using System;
using Genesis.Architecture;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Genesis.GameStates.Overlays.Tutorial;

/// <summary>
/// Renders the tutorial overlay including panel, header, text, and scroll hint
/// </summary>
public class TutorialRenderer
{
    private readonly Texture2D mPanelTexture;
    private readonly Texture2D mOverlayPixel;
    private readonly SpriteFont mFont;
    private readonly TutorialLayoutCalculator mLayoutCalculator;

    public TutorialRenderer(Texture2D panelTexture, Texture2D overlayPixel, SpriteFont font)
    {
        mPanelTexture = panelTexture ?? throw new ArgumentNullException(nameof(panelTexture));
        mOverlayPixel = overlayPixel ?? throw new ArgumentNullException(nameof(overlayPixel));
        mFont = font ?? throw new ArgumentNullException(nameof(font));
        mLayoutCalculator = new TutorialLayoutCalculator(panelTexture);
    }

    public void DrawOverlay(
        SpriteBatch spriteBatch,
        float screenCenterX,
        float screenCenterY,
        string visibleText,
        bool showScrollHint,
        bool scrollHintVisible)
    {
        var panelPosition = mLayoutCalculator.CalculatePanelPosition(screenCenterX, screenCenterY);
        var headerSize = mFont.MeasureString(TutorialContent.Title) * TutorialLayoutSettings.HeaderScale;
        var headerPosition = mLayoutCalculator.CalculateHeaderPosition(panelPosition, headerSize);
        var textFieldBounds = mLayoutCalculator.CalculateTextFieldBounds(panelPosition);
        var textPosition = mLayoutCalculator.CalculateTextPosition(textFieldBounds);

        // Dark overlay
        var overlayBounds = new Rectangle(0, 0, ScreenService.VirtualWidth, ScreenService.VirtualHeight);
        var overlayColor = Color.Black * 0.75f;

        DrawDarkOverlay(spriteBatch, overlayBounds, overlayColor);
        DrawPanel(spriteBatch, panelPosition);
        DrawHeader(spriteBatch, headerPosition);
        DrawTextFieldBackground(spriteBatch, textFieldBounds);
        DrawBodyText(spriteBatch, visibleText, textPosition);

        if (showScrollHint && scrollHintVisible)
        {
            DrawScrollHint(spriteBatch, textPosition, textFieldBounds);
        }
    }

    private void DrawDarkOverlay(SpriteBatch spriteBatch, Rectangle bounds, Color color)
    {
        spriteBatch.Draw(mOverlayPixel, bounds, null, color, 0f, Vector2.Zero, SpriteEffects.None, 0.1f);
    }

    private void DrawPanel(SpriteBatch spriteBatch, Vector2 position)
    {
        spriteBatch.Draw(mPanelTexture, position, null, Color.White, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0.2f);
    }

    private void DrawHeader(SpriteBatch spriteBatch, Vector2 position)
    {
        spriteBatch.DrawString(
            mFont,
            "Tutorial",
            position,
            Color.Black,
            0f,
            Vector2.Zero,
            TutorialLayoutSettings.HeaderScale,
            SpriteEffects.None,
            0.3f);
    }

    private void DrawTextFieldBackground(SpriteBatch spriteBatch, Rectangle bounds)
    {
        spriteBatch.Draw(mOverlayPixel, bounds, null, Color.Transparent * 0.6f, 0f, Vector2.Zero, SpriteEffects.None, 0.3f);
    }

    private void DrawBodyText(SpriteBatch spriteBatch, string text, Vector2 position)
    {
        spriteBatch.DrawString(
            mFont,
            text,
            position,
            Color.Black,
            0f,
            Vector2.Zero,
            TutorialLayoutSettings.BodyTextScale,
            SpriteEffects.None,
            0.4f);
    }

    private void DrawScrollHint(SpriteBatch spriteBatch, Vector2 textPosition, Rectangle textFieldBounds)
    {
        var glyphSize = mFont.MeasureString(TutorialLayoutSettings.ScrollContinuationGlyph);
        var scaledGlyphWidth = glyphSize.X * TutorialLayoutSettings.GlyphScale;

        var glyphPosition = new Vector2(
            textFieldBounds.X + (textFieldBounds.Width - scaledGlyphWidth) * 0.5f,
            textFieldBounds.Bottom - glyphSize.Y * TutorialLayoutSettings.BodyTextScale - TutorialLayoutSettings.TextBoxPaddingY);

        spriteBatch.DrawString(
            mFont,
            TutorialLayoutSettings.ScrollContinuationGlyph,
            glyphPosition,
            Color.Orange,
            0f,
            Vector2.Zero,
            TutorialLayoutSettings.GlyphScale,
            SpriteEffects.None,
            0.45f);
    }
}