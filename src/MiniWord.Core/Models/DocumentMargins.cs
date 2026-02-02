using MiniWord.Core.Exceptions;
using Serilog;

namespace MiniWord.Core.Models;

/// <summary>
/// Represents the margins of a document in pixels
/// </summary>
public class DocumentMargins
{
    private static readonly ILogger _logger = Log.ForContext<DocumentMargins>();
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
            {
                var ex = new MarginException($"Left margin cannot be negative. Attempted value: {value}");
                _logger.Error(ex, "Invalid left margin value: {Value}", value);
                throw ex;
            }
            _left = value;
        }
    }

    public double Right
    {
        get => _right;
        set
        {
            if (value < 0)
            {
                var ex = new MarginException($"Right margin cannot be negative. Attempted value: {value}");
                _logger.Error(ex, "Invalid right margin value: {Value}", value);
                throw ex;
            }
            _right = value;
        }
    }

    public double Top
    {
        get => _top;
        set
        {
            if (value < 0)
            {
                var ex = new MarginException($"Top margin cannot be negative. Attempted value: {value}");
                _logger.Error(ex, "Invalid top margin value: {Value}", value);
                throw ex;
            }
            _top = value;
        }
    }

    public double Bottom
    {
        get => _bottom;
        set
        {
            if (value < 0)
            {
                var ex = new MarginException($"Bottom margin cannot be negative. Attempted value: {value}");
                _logger.Error(ex, "Invalid bottom margin value: {Value}", value);
                throw ex;
            }
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
