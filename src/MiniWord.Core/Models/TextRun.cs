namespace MiniWord.Core.Models;

/// <summary>
/// Represents a continuous segment of text with consistent formatting.
/// This is the smallest unit of formatted text in the document.
/// </summary>
public class TextRun
{
    /// <summary>
    /// The actual text content of this run.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Font family name (e.g., "Arial", "Times New Roman").
    /// </summary>
    public string FontFamily { get; set; } = "Segoe UI";

    /// <summary>
    /// Font size in points.
    /// </summary>
    public double FontSize { get; set; } = 12.0;

    /// <summary>
    /// Indicates if the text is bold.
    /// </summary>
    public bool IsBold { get; set; } = false;

    /// <summary>
    /// Indicates if the text is italic.
    /// </summary>
    public bool IsItalic { get; set; } = false;

    /// <summary>
    /// Indicates if the text is underlined.
    /// </summary>
    public bool IsUnderline { get; set; } = false;

    /// <summary>
    /// Text color in hex format (e.g., "#000000" for black).
    /// </summary>
    public string Color { get; set; } = "#000000";

    /// <summary>
    /// Creates a copy of this TextRun with the same formatting properties.
    /// </summary>
    public TextRun Clone()
    {
        return new TextRun
        {
            Text = Text,
            FontFamily = FontFamily,
            FontSize = FontSize,
            IsBold = IsBold,
            IsItalic = IsItalic,
            IsUnderline = IsUnderline,
            Color = Color
        };
    }
}
