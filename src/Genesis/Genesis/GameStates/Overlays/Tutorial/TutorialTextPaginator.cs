using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace Genesis.GameStates.Overlays.Tutorial;

/// <summary>
/// Contains logic to paginate and wrap tutorial text for display in the tutorial overlay.
/// </summary>
public class TutorialTextPaginator
{
    // Characters that define word boundaries for wrapping
    private static readonly char[] WordBoundaryCharacters = { ' ', '\r', '\n', '\t' };
    
    private readonly List<string> mPages = new();
    private readonly SpriteFont mFont;
    private readonly int mMaxCharactersPerLine;
    private readonly int mMaxCharactersPerPage;
    
    private int mCurrentPageIndex;

    public TutorialTextPaginator(SpriteFont font, int maxCharactersPerLine, int maxCharactersPerPage)
    {
        mFont = font ?? throw new ArgumentNullException(nameof(font));
        mMaxCharactersPerLine = maxCharactersPerLine;
        mMaxCharactersPerPage = maxCharactersPerPage;
    }

    public void BuildPages(string fullText, float maxLineWidth)
    {
        mPages.Clear();
        mCurrentPageIndex = 0;

        if (string.IsNullOrWhiteSpace(fullText))
        {
            mPages.Add(string.Empty);
            return;
        }

        var remaining = fullText;
        
        while (!string.IsNullOrEmpty(remaining))
        {
            var (segment, consumedLength) = ExtractSegment(remaining);
            
            if (consumedLength == 0)
            {
                break;
            }

            if (!string.IsNullOrWhiteSpace(segment))
            {
                mPages.Add(WrapText(segment, maxLineWidth));
            }

            remaining = consumedLength < remaining.Length
                ? remaining[consumedLength..].TrimStart()
                : string.Empty;
        }

        if (mPages.Count == 0)
        {
            mPages.Add(WrapText(fullText, maxLineWidth));
        }
    }

    public string GetCurrentPageText()
    {
        if (mPages.Count == 0)
        {
            return string.Empty;
        }

        var index = Math.Clamp(mCurrentPageIndex, 0, mPages.Count - 1);
        return mPages[index];
    }

    public bool TryAdvanceToNextPage()
    {
        if (!HasNextPage())
        {
            return false;
        }

        mCurrentPageIndex++;
        return true;
    }

    public bool TryGoToPreviousPage()
    {
        if (!HasPreviousPage())
        {
            return false;
        }

        mCurrentPageIndex--;
        return true;
    }

    public bool HasNextPage()
    {
        return mPages.Count > 0 && mCurrentPageIndex < mPages.Count - 1;
    }

    public bool HasPreviousPage()
    {
        return mCurrentPageIndex > 0;
    }

    public void Reset()
    {
        mCurrentPageIndex = 0;
    }

    /// <summary>
    /// Contracts a segment of text that fits within the maximum characters per page,
    /// and attempts to break at word boundaries.
    /// </summary>
    private (string segment, int consumedLength) ExtractSegment(string source)
    {
        if (string.IsNullOrEmpty(source))
        {
            return (string.Empty, 0);
        }

        var allowedLength = Math.Min(mMaxCharactersPerPage, source.Length);
        var segment = source[..allowedLength];

        if (allowedLength < source.Length && !char.IsWhiteSpace(source[allowedLength]))
        {
            var lastWhitespace = segment.LastIndexOfAny(WordBoundaryCharacters);
            if (lastWhitespace >= 0)
            {
                allowedLength = lastWhitespace + 1;
                segment = source[..allowedLength];
            }
        }

        return (segment.TrimEnd(), allowedLength);
    }

    /// <summary>
    /// Wraps the given text to fit within the specified maximum line width.
    /// <returns></returns>
    private string WrapText(string text, float maxLineWidth)
    {
        if (string.IsNullOrWhiteSpace(text) || maxLineWidth <= 0f)
        {
            return text;
        }

        var paragraphs = text.ReplaceLineEndings("\n").Split('\n');
        var builder = new StringBuilder();

        for (int i = 0; i < paragraphs.Length; i++)
        {
            var paragraph = paragraphs[i];
            
            if (string.IsNullOrWhiteSpace(paragraph))
            {
                builder.AppendLine();
            }
            else
            {
                WrapParagraph(paragraph, builder, maxLineWidth);
                
                if (i < paragraphs.Length - 1)
                {
                    builder.AppendLine();
                }
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Wraps a single paragraph into lines that fit within the maximum line width.
    /// </summary>
    private void WrapParagraph(string paragraph, StringBuilder builder, float maxLineWidth)
    {
        var words = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var currentLine = string.Empty;

        foreach (var word in words)
        {
            var candidateLine = string.IsNullOrEmpty(currentLine) ? word : $"{currentLine} {word}";
            
            if (FitsInLine(candidateLine, maxLineWidth))
            {
                currentLine = candidateLine;
                continue;
            }

            if (!string.IsNullOrEmpty(currentLine))
            {
                builder.AppendLine(currentLine);
            }

            if (FitsInLine(word, maxLineWidth))
            {
                currentLine = word;
            }
            else
            {
                BreakLongWord(word, builder, maxLineWidth);
                currentLine = string.Empty;
            }
        }

        if (!string.IsNullOrEmpty(currentLine))
        {
            builder.Append(currentLine);
        }
    }

    /// <summary>
    /// Cuts a long word into smaller segments that fit within the maximum line width.
    /// </summary>
    private void BreakLongWord(string word, StringBuilder builder, float maxLineWidth)
    {
        if (string.IsNullOrEmpty(word))
        {
            return;
        }

        var startIndex = 0;
        
        while (startIndex < word.Length)
        {
            var remainingLength = word.Length - startIndex;
            var chunkLength = Math.Min(mMaxCharactersPerLine, remainingLength);
            var chunk = word.Substring(startIndex, chunkLength);

            while (chunkLength > 1 && mFont.MeasureString(chunk).X > maxLineWidth)
            {
                chunkLength--;
                chunk = word.Substring(startIndex, chunkLength);
            }

            if (chunkLength == 0)
            {
                chunkLength = 1;
                chunk = word.Substring(startIndex, 1);
            }

            builder.AppendLine(chunk);
            startIndex += chunkLength;
        }
    }

    /// <summary>
    /// Checks if the given text fits within the specified maximum line width.
    /// <returns></returns>
    private bool FitsInLine(string text, float maxLineWidth)
    {
        if (string.IsNullOrEmpty(text))
        {
            return true;
        }

        if (text.Length > mMaxCharactersPerLine)
        {
            return false;
        }

        return mFont.MeasureString(text).X <= maxLineWidth;
    }
}