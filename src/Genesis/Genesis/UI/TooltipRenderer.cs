using System.Collections.Generic;
using System.Text;
using Arch.Core;
using Genesis.Architecture;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Definitions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Genesis.UI;

public class TooltipRenderer
{
    private readonly GameServices mServices;
    private readonly Texture2D mPixelTexture;
    private readonly SpriteFont mFont;

    // Design Constants
    private const float TooltipWidth = 550f;
    
    private const int PaddingX = 20;
    private const int PaddingY = 20;
    private const int TitleToDescGap = 20;

    public TooltipRenderer(GameServices services, ScreenService screen)
    {
        mServices = services;
        mFont = services.Content.Load<SpriteFont>("Fonts/HudFont");
        mPixelTexture = new Texture2D(screen.Graphics, 1, 1);
        mPixelTexture.SetData([Color.White]);
    }
    
    /// <summary>
    /// Creates a tooltip next to the mouse position und and returns a list of the created entities.
    /// The caller is responsible to destroy the entities, when the tooltip should be removed.
    /// </summary>
    public List<Entity> CreateItemTooltip(World uiWorld, ItemDefinition def, Vector2 mousePosition)
    {
        var createdEntities = new List<Entity>();
    
        const float cropTop = 15f;
        const float cropBottom = 7f;
        const float fontVisualCorrection = cropTop + cropBottom;

        var title = def.Name;
        var wrappedDesc = WrapText(mFont, def.Description, TooltipWidth - (PaddingX * 2));

        // Stats
        var statsParts = new List<string>();
        if (def.Damage > 0) {statsParts.Add($"Damage: {def.Damage}");}
        if (def.AttackRange > 0) {statsParts.Add($"Range: {FormatRange(def.AttackRange)}");}
        if (def.Cooldown > 0) {statsParts.Add($"Cooldown: {def.Cooldown}s");}
        if (def.AoeDamage > 0) {statsParts.Add($"AOE Damage: {def.AoeDamage}");}
        if (def.AoeRange > 0) {statsParts.Add($"AOE Range: {def.AoeRange}");}
        
        var sbStats = new StringBuilder();
        var currentLineWidth = 0f;
        const float maxStatWidth = TooltipWidth - (PaddingX * 2);
        const string separator = "   "; // Abstand zwischen den Stats
        var separatorWidth = mFont.MeasureString(separator).X;

        for (var i = 0; i < statsParts.Count; i++)
        {
            var part = statsParts[i];
            var partSize = mFont.MeasureString(part);

            if (i == 0)
            {
                sbStats.Append(part);
                currentLineWidth += partSize.X;
            }
            else
            {
                if (currentLineWidth + separatorWidth + partSize.X <= maxStatWidth)
                {
                    sbStats.Append(separator);
                    sbStats.Append(part);
                    currentLineWidth += separatorWidth + partSize.X;
                }
                else
                {
                    sbStats.Append("\n");
                    sbStats.Append(part);
                    currentLineWidth = partSize.X;
                }
            }
        }
        
        var statsText = sbStats.ToString();

        // Visible Height
        var titleFullSize = mFont.MeasureString(title);
        var titleVisibleHeight = titleFullSize.Y - fontVisualCorrection;

        var descFullSize = mFont.MeasureString(wrappedDesc);
        var descVisibleHeight = descFullSize.Y - fontVisualCorrection;

        float statsVisibleHeight = 0;
        if (!string.IsNullOrEmpty(statsText))
        {
            statsVisibleHeight = mFont.MeasureString(statsText).Y - fontVisualCorrection;
        }

        // Total height
        var totalHeight = titleVisibleHeight + TitleToDescGap + descVisibleHeight + (PaddingY * 2);
        if (statsVisibleHeight > 0)
        {
            totalHeight += TitleToDescGap + statsVisibleHeight;
        }

        // Position
        var boxTopLeft = mousePosition + new Vector2(20, 20);
        if (boxTopLeft.X + TooltipWidth > ScreenService.VirtualWidth)
        {
            boxTopLeft.X = mousePosition.X - TooltipWidth - 10;
        }

        if (boxTopLeft.Y + totalHeight > ScreenService.VirtualHeight)
        {
            boxTopLeft.Y = mousePosition.Y - totalHeight - 10;
        }

        if (boxTopLeft.X < 5) {boxTopLeft.X = 5;}
        if (boxTopLeft.Y < 5) {boxTopLeft.Y = 5;}

        // Background
        var centerPos = boxTopLeft + new Vector2(TooltipWidth / 2f, totalHeight / 2f);
        var bgEntity = mServices.UiFactory.MarkAsStaticUi(uiWorld, uiWorld.Create());
        uiWorld.Add(bgEntity,
            new PositionComponent(centerPos),
            new SpriteComponent(mPixelTexture, new Rectangle(0, 0, (int)TooltipWidth, (int)totalHeight), 0.98f)
            {
                mColor = new Color(15, 15, 15, 245),
                Origin = new Vector2(TooltipWidth / 2f, totalHeight / 2f)
            },
            new IsVisibleComponent(),
            new IgnoreCullingComponent()
        );
        createdEntities.Add(bgEntity);

        // Title
        var titleEntity = mServices.UiFactory.MarkAsStaticUi(uiWorld, uiWorld.Create());
        uiWorld.Add(titleEntity,
            new PositionComponent(boxTopLeft + new Vector2(PaddingX, PaddingY)),
            new TextComponent(title, mFont, Color.Gold, TextAlignment.TopLeft),
            new IsVisibleComponent(),
            new IgnoreCullingComponent()
        );
        createdEntities.Add(titleEntity);

        // Description
        var descPos = boxTopLeft + new Vector2(PaddingX, PaddingY + titleVisibleHeight + TitleToDescGap);
        var descEntity = mServices.UiFactory.MarkAsStaticUi(uiWorld, uiWorld.Create());
        uiWorld.Add(descEntity,
            new PositionComponent(descPos),
            new TextComponent(wrappedDesc, mFont, Color.LightGray, TextAlignment.TopLeft),
            new IsVisibleComponent(),
            new IgnoreCullingComponent()
        );
        createdEntities.Add(descEntity);

        // 9. Stats-Zeile erstellen (falls vorhanden)
        if (statsVisibleHeight > 0)
        {
            var statsPos = descPos + new Vector2(0, descVisibleHeight + TitleToDescGap);
            var statsEntity = mServices.UiFactory.MarkAsStaticUi(uiWorld, uiWorld.Create());
            uiWorld.Add(statsEntity,
                new PositionComponent(statsPos),
                new TextComponent(statsText, mFont, Color.Orange, TextAlignment.TopLeft),
                new IsVisibleComponent(),
                new IgnoreCullingComponent()
            );
            createdEntities.Add(statsEntity);
        }

        return createdEntities;

        string FormatRange(float value)
        {
            return value > 999 ? "Inf" : value.ToString();
        }
    }
    
    private string WrapText(SpriteFont font, string text, float maxWidth)
    {
        if (string.IsNullOrEmpty(text)) { return string.Empty; }

        var words = text.Split(' ');
        var sb = new StringBuilder();
        var lineWidth = 0f;
        var spaceWidth = font.MeasureString(" ").X;

        foreach (var word in words)
        {
            var size = font.MeasureString(word);

            if (lineWidth + size.X < maxWidth)
            {
                sb.Append(word + " ");
                lineWidth += size.X + spaceWidth;
            }
            else
            {
                sb.Append("\n" + word + " ");
                lineWidth = size.X + spaceWidth;
            }
        }
        return sb.ToString().TrimEnd();
    }
    
    public void Dispose()
    {
        mPixelTexture?.Dispose();
    }
}