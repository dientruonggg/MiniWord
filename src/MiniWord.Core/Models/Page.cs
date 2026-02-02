using Serilog;

namespace MiniWord.Core.Models;

/// <summary>
/// Represents a single page in a multi-page document
/// </summary>
public class Page
{
    /// <summary>
    /// Page number (1-based index)
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Text lines contained in this page
    /// </summary>
    public List<TextLine> Lines { get; set; } = new List<TextLine>();

    /// <summary>
    /// Page content as string
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Whether this page has content
    /// </summary>
    public bool HasContent => !string.IsNullOrWhiteSpace(Content) || Lines.Count > 0;

    public Page()
    {
    }

    public Page(int pageNumber)
    {
        PageNumber = pageNumber;
    }

    public Page(int pageNumber, string content)
    {
        PageNumber = pageNumber;
        Content = content;
    }

    public override string ToString()
    {
        return $"Page {PageNumber}: {Lines.Count} lines, {Content?.Length ?? 0} characters";
    }
}
