namespace MiniWord.Core.Models;

/// <summary>
/// Represents the margins of a document in pixels
/// </summary>
public class DocumentMargins
{
    private double _left;
    private double _right;
    private double _top;
    private double _bottom;

    public double Left
    {
        get => _left;
        set
        {
            if (value < 0)
                throw new ArgumentException("Left margin cannot be negative", nameof(Left));
            _left = value;
        }
    }

    public double Right
    {
        get => _right;
        set
        {
            if (value < 0)
                throw new ArgumentException("Right margin cannot be negative", nameof(Right));
            _right = value;
        }
    }

    public double Top
    {
        get => _top;
        set
        {
            if (value < 0)
                throw new ArgumentException("Top margin cannot be negative", nameof(Top));
            _top = value;
        }
    }

    public double Bottom
    {
        get => _bottom;
        set
        {
            if (value < 0)
                throw new ArgumentException("Bottom margin cannot be negative", nameof(Bottom));
            _bottom = value;
        }
    }

    /// <summary>
    /// Creates default margins (1 inch = 96 pixels at 96 DPI)
    /// </summary>
    public DocumentMargins() : this(96, 96, 96, 96)
    {
    }

    /// <summary>
    /// Creates margins with specified values
    /// </summary>
    public DocumentMargins(double left, double right, double top, double bottom)
    {
        Left = left;
        Right = right;
        Top = top;
        Bottom = bottom;
    }

    /// <summary>
    /// Calculates the total horizontal margin (left + right)
    /// </summary>
    public double TotalHorizontal => Left + Right;

    /// <summary>
    /// Calculates the total vertical margin (top + bottom)
    /// </summary>
    public double TotalVertical => Top + Bottom;

    public override string ToString()
    {
        return $"L:{Left:F1}, R:{Right:F1}, T:{Top:F1}, B:{Bottom:F1}";
    }
}
