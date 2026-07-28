namespace Genesis.GameStates.Overlays.Tutorial;

/// <summary>
/// Settings for the layout of the tutorial overlay.
/// </summary>
public static class TutorialLayoutSettings
{
    // Panel settings
    public const float PanelWidthRatio = 0.78125f;
    public const float PanelHeightRatio = 0.50f;

    // Header settings
    public const float HeaderScale = 2.0f;
    public const float HeaderOffsetY = 30f;

    // Text box settings
    public const float TextBoxOffsetYRatio = 0.21f;
    public const float TextBoxHeightRatio = 0.50f;
    public const float InnerMarginRatio = 0.14375f;
    public const float BodyTextScale = 1.2f;
    public const float TextBoxPaddingX = 55f;
    public const float TextBoxPaddingY = 0f;

    // Button settings
    public const float ButtonWidthRatio = 1f / 6f;
    public const float ButtonHeightRatio = 1f / 30f;
    public const float ButtonYOffsetFromBottom = 170f;

    // Typewriter settings
    public const double TypewriterSpeed = 0.01;
    public const string ScrollContinuationGlyph = "(Scroll or use the arrow keys to continue reading)";
    public const float GlyphScale = 1.1f;
    public const double ScrollGlyphBlinkInterval = 2.0;
}