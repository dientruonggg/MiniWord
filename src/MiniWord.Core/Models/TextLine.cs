namespace MiniWord.Core.Models;

/// <summary>
/// Represents a single line of text in the document
/// </summary>
public class TextLine
{
    /// <summary>
    /// The text content of this line
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// The starting index of this line in the original text
    /// </summary>
    public int StartIndex { get; set; }

    /// <summary>
    /// The length of this line (including whitespace)
    /// </summary>
    public int Length => Content.Length;

    /// <summary>
    /// The measured width of this line in pixels
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// Whether this line ends with a hard break (e.g., Enter key)
    /// </summary>
    public bool IsHardBreak { get; set; }

    /// <summary>
    /// List of formatting spans applied to this line (P5.3)
    /// </summary>
    public List<FormattingSpan> FormattingSpans { get; set; } = new List<FormattingSpan>();

    public TextLine()
    {
    }

    public TextLine(string content, int startIndex, double width, bool isHardBreak = false)
    {
        Content = content;
        StartIndex = startIndex;
        Width = width;
        IsHardBreak = isHardBreak;
    }

    public override string ToString()
    {
        return $"Line[{StartIndex}]: \"{Content}\" ({Width:F1}px)";
    }
}

