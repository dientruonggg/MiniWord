using System;

namespace MiniWord.Core.Models;

/// <summary>
/// Represents a range of text in a document with start and end positions.
/// Used for search results, formatting spans, and text manipulation.
/// Similar to TextSelection but focused on readonly range representation.
/// </summary>
public class TextRange
{
    /// <summary>
    /// Gets the start position of the range (0-based index, inclusive)
    /// </summary>
    public int Start { get; }

    /// <summary>
    /// Gets the end position of the range (0-based index, exclusive)
    /// </summary>
    public int End { get; }

    /// <summary>
    /// Gets the length of the range
    /// </summary>
    public int Length => Math.Max(0, End - Start);

    /// <summary>
    /// Gets whether this range is empty (zero length)
    /// </summary>
    public bool IsEmpty => Start == End;

    /// <summary>
    /// Creates a new text range
    /// </summary>
    /// <param name="start">Start position (0-based index, inclusive)</param>
    /// <param name="end">End position (0-based index, exclusive)</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when start or end is negative, or start > end</exception>
    public TextRange(int start, int end)
    {
        if (start < 0)
            throw new ArgumentOutOfRangeException(nameof(start), "Start position cannot be negative");
        if (end < 0)
            throw new ArgumentOutOfRangeException(nameof(end), "End position cannot be negative");
        if (start > end)
            throw new ArgumentOutOfRangeException(nameof(start), "Start position cannot be greater than end position");

        Start = start;
        End = end;
    }

    /// <summary>
    /// Creates a text range from start position and length
    /// </summary>
    /// <param name="start">Start position (0-based index)</param>
    /// <param name="length">Length of the range</param>
    /// <returns>A new TextRange</returns>
    public static TextRange FromStartAndLength(int start, int length)
    {
        if (start < 0)
            throw new ArgumentOutOfRangeException(nameof(start), "Start position cannot be negative");
        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length), "Length cannot be negative");

        return new TextRange(start, start + length);
    }

    /// <summary>
    /// Checks if this range contains the specified position
    /// </summary>
    /// <param name="position">The position to check</param>
    /// <returns>True if the position is within this range</returns>
    public bool Contains(int position)
    {
        return position >= Start && position < End;
    }

    /// <summary>
    /// Checks if this range intersects with another range
    /// </summary>
    /// <param name="other">The other range to check</param>
    /// <returns>True if the ranges intersect</returns>
    public bool IntersectsWith(TextRange other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));

        return Start < other.End && End > other.Start;
    }

    public override string ToString()
    {
        return IsEmpty ? $"Empty({Start})" : $"Range({Start}-{End}, Length={Length})";
    }

    public override bool Equals(object? obj)
    {
        return obj is TextRange other && Start == other.Start && End == other.End;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Start, End);
    }
}
