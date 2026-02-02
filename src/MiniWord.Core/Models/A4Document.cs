using Serilog;

namespace MiniWord.Core.Models;

/// <summary>
/// Represents an A4 document (210mm x 297mm)
/// At 96 DPI: 794 x 1123 pixels
/// </summary>
public class A4Document
{
    private readonly ILogger _logger;

    /// <summary>
    /// A4 width in pixels at 96 DPI (210mm)
    /// </summary>
    public const double A4_WIDTH_PX = 794;

    /// <summary>
    /// A4 height in pixels at 96 DPI (297mm)
    /// </summary>
    public const double A4_HEIGHT_PX = 1123;

    /// <summary>
    /// Document margins
    /// </summary>
    public DocumentMargins Margins { get; private set; }

    /// <summary>
    /// Document content
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Available width for text (paper width - margins)
    /// </summary>
    public double AvailableWidth => A4_WIDTH_PX - Margins.TotalHorizontal;

    /// <summary>
    /// Available height for text (paper height - margins)
    /// </summary>
    public double AvailableHeight => A4_HEIGHT_PX - Margins.TotalVertical;

    /// <summary>
    /// Event raised when margins are changed
    /// </summary>
    public event EventHandler<MarginsChangedEventArgs>? MarginsChanged;

    public A4Document(ILogger logger)
    {
        _logger = logger;
        Margins = new DocumentMargins();
        _logger.Information("A4 Document created with dimensions {Width}x{Height}px, margins: {Margins}",
            A4_WIDTH_PX, A4_HEIGHT_PX, Margins);
    }

    /// <summary>
    /// Updates document margins and raises MarginsChanged event
    /// </summary>
    public void UpdateMargins(DocumentMargins newMargins)
    {
        if (newMargins.TotalHorizontal >= A4_WIDTH_PX)
        {
            _logger.Error("Invalid margins: Total horizontal margin {Total} exceeds page width {Width}",
                newMargins.TotalHorizontal, A4_WIDTH_PX);
            throw new ArgumentException("Total horizontal margins cannot exceed page width");
        }

        if (newMargins.TotalVertical >= A4_HEIGHT_PX)
        {
            _logger.Error("Invalid margins: Total vertical margin {Total} exceeds page height {Height}",
                newMargins.TotalVertical, A4_HEIGHT_PX);
            throw new ArgumentException("Total vertical margins cannot exceed page height");
        }

        var oldMargins = Margins;
        Margins = newMargins;

        _logger.Information("Margins updated from {OldMargins} to {NewMargins}. Available width: {Width}px",
            oldMargins, newMargins, AvailableWidth);

        MarginsChanged?.Invoke(this, new MarginsChangedEventArgs(oldMargins, newMargins));
    }

    /// <summary>
    /// Validates that the given width is within document bounds
    /// </summary>
    public bool IsValidWidth(double width)
    {
        return width > 0 && width <= AvailableWidth;
    }
}

/// <summary>
/// Event arguments for margin changes
/// </summary>
public class MarginsChangedEventArgs : EventArgs
{
    public DocumentMargins OldMargins { get; }
    public DocumentMargins NewMargins { get; }

    public MarginsChangedEventArgs(DocumentMargins oldMargins, DocumentMargins newMargins)
    {
        OldMargins = oldMargins;
        NewMargins = newMargins;
    }
}
