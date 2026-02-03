namespace MiniWord.Core.Models;

/// <summary>
/// Represents a formatting style that can be applied to text
/// </summary>
[Flags]
public enum TextFormatting
{
    /// <summary>
    /// No formatting applied
    /// </summary>
    None = 0,
    
    /// <summary>
    /// Bold text
    /// </summary>
    Bold = 1 << 0,
    
    /// <summary>
    /// Italic text
    /// </summary>
    Italic = 1 << 1,
    
    /// <summary>
    /// Underlined text
    /// </summary>
    Underline = 1 << 2
}

/// <summary>
/// Represents a range of text with specific formatting applied
/// </summary>
public class FormattingSpan
{
    /// <summary>
    /// The starting index of the formatting span (relative to the line or document)
    /// </summary>
    public int StartIndex { get; set; }
    
    /// <summary>
    /// The length of the formatting span
    /// </summary>
    public int Length { get; set; }
    
    /// <summary>
    /// The formatting styles applied to this span
    /// </summary>
    public TextFormatting Formatting { get; set; }
    
    /// <summary>
    /// Gets the end index (exclusive) of this span
    /// </summary>
    public int EndIndex => StartIndex + Length;
    
    /// <summary>
    /// Creates a new formatting span
    /// </summary>
    public FormattingSpan()
    {
        StartIndex = 0;
        Length = 0;
        Formatting = TextFormatting.None;
    }
    
    /// <summary>
    /// Creates a new formatting span with specified values
    /// </summary>
    /// <param name="startIndex">The starting index of the span</param>
    /// <param name="length">The length of the span</param>
    /// <param name="formatting">The formatting to apply</param>
    public FormattingSpan(int startIndex, int length, TextFormatting formatting)
    {
        StartIndex = startIndex;
        Length = length;
        Formatting = formatting;
    }
    
    /// <summary>
    /// Checks if this span overlaps with another span
    /// </summary>
    /// <param name="other">The other span to check</param>
    /// <returns>True if the spans overlap</returns>
    public bool OverlapsWith(FormattingSpan other)
    {
        return StartIndex < other.EndIndex && EndIndex > other.StartIndex;
    }
    
    /// <summary>
    /// Checks if a specific index is contained within this span
    /// </summary>
    /// <param name="index">The index to check</param>
    /// <returns>True if the index is within this span</returns>
    public bool Contains(int index)
    {
        return index >= StartIndex && index < EndIndex;
    }
    
    public override string ToString()
    {
        return $"Span[{StartIndex}-{EndIndex}]: {Formatting}";
    }
}
