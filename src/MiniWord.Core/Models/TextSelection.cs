using System;

namespace MiniWord.Core.Models;

/// <summary>
/// Represents a text selection with start and end positions
/// Maps WinForms TextBox.SelectionStart/Length to Avalonia equivalents
/// </summary>
public class TextSelection
{
    /// <summary>
    /// Gets the start position of the selection (0-based index)
    /// </summary>
    public int Start { get; }

    /// <summary>
    /// Gets the end position of the selection (0-based index, exclusive)
    /// </summary>
    public int End { get; }

    /// <summary>
    /// Gets the length of the selection
    /// </summary>
    public int Length => Math.Max(0, End - Start);

    /// <summary>
    /// Gets whether this selection is empty (no text selected)
    /// </summary>
    public bool IsEmpty => Start == End;

    /// <summary>
    /// Creates a new text selection
    /// </summary>
    /// <param name="start">Start position (0-based index)</param>
    /// <param name="end">End position (0-based index, exclusive)</param>
    public TextSelection(int start, int end)
    {
        if (start < 0)
            throw new ArgumentOutOfRangeException(nameof(start), "Start position cannot be negative");
        if (end < 0)
            throw new ArgumentOutOfRangeException(nameof(end), "End position cannot be negative");

        // Normalize so Start is always <= End
        if (start <= end)
        {
            Start = start;
            End = end;
        }
        else
        {
            Start = end;
            End = start;
        }
    }

    /// <summary>
    /// Creates an empty selection at the specified position
    /// </summary>
    public static TextSelection Empty(int position = 0)
    {
        return new TextSelection(position, position);
    }

    /// <summary>
    /// Creates a selection from start position and length (WinForms compatibility)
    /// </summary>
    public static TextSelection FromStartAndLength(int start, int length)
    {
        if (start < 0)
            throw new ArgumentOutOfRangeException(nameof(start), "Start position cannot be negative");
        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length), "Length cannot be negative");

        return new TextSelection(start, start + length);
    }

    /// <summary>
    /// Checks if this selection contains the specified position
    /// </summary>
    public bool Contains(int position)
    {
        return position >= Start && position < End;
    }

    /// <summary>
    /// Checks if this selection intersects with another selection
    /// </summary>
    public bool IntersectsWith(TextSelection other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));

        return Start < other.End && End > other.Start;
    }

    public override string ToString()
    {
        return IsEmpty ? $"Empty({Start})" : $"Selection({Start}-{End}, Length={Length})";
    }

    public override bool Equals(object? obj)
    {
        return obj is TextSelection other && Start == other.Start && End == other.End;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Start, End);
    }
}
