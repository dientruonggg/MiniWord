namespace MiniWord.Core.Models;

/// <summary>
/// Represents a paragraph in the document, containing one or more text runs.
/// A paragraph is a block-level element that can contain formatted text.
/// </summary>
public class Paragraph
{
    /// <summary>
    /// Collection of text runs that make up this paragraph.
    /// </summary>
    public List<TextRun> Runs { get; set; } = new();

    /// <summary>
    /// Text alignment within the paragraph.
    /// </summary>
    public TextAlignment Alignment { get; set; } = TextAlignment.Left;

    /// <summary>
    /// Line spacing multiplier (1.0 = single, 1.5 = 1.5x, 2.0 = double).
    /// </summary>
    public double LineSpacing { get; set; } = 1.0;

    /// <summary>
    /// Left indentation in points.
    /// </summary>
    public double LeftIndent { get; set; } = 0.0;

    /// <summary>
    /// Right indentation in points.
    /// </summary>
    public double RightIndent { get; set; } = 0.0;

    /// <summary>
    /// Spacing before the paragraph in points.
    /// </summary>
    public double SpacingBefore { get; set; } = 0.0;

    /// <summary>
    /// Spacing after the paragraph in points.
    /// </summary>
    public double SpacingAfter { get; set; } = 0.0;

    /// <summary>
    /// Gets the plain text content of the entire paragraph.
    /// </summary>
    public string GetText()
    {
        return string.Join("", Runs.Select(r => r.Text));
    }

    /// <summary>
    /// Adds a new text run to the paragraph.
    /// </summary>
    public void AddRun(TextRun run)
    {
        Runs.Add(run);
    }

    /// <summary>
    /// Checks if the paragraph is empty (no runs or all runs are empty).
    /// </summary>
    public bool IsEmpty()
    {
        return !Runs.Any() || Runs.All(r => string.IsNullOrEmpty(r.Text));
    }
}

/// <summary>
/// Text alignment options for paragraphs.
/// </summary>
public enum TextAlignment
{
    Left,
    Center,
    Right,
    Justify
}
