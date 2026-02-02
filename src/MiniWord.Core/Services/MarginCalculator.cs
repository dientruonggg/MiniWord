using MiniWord.Core.Models;
using Serilog;

namespace MiniWord.Core.Services;

/// <summary>
/// Service for calculating margin-related measurements
/// </summary>
public class MarginCalculator
{
    private readonly ILogger _logger;

    public MarginCalculator(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Calculates the effective writing area width given paper width and margins
    /// </summary>
    public double CalculateAvailableWidth(double paperWidth, DocumentMargins margins)
    {
        _logger.Debug("Calculating available width: PaperWidth={PaperWidth}, Margins={Margins}",
            paperWidth, margins);

        if (paperWidth <= 0)
        {
            _logger.Error("Invalid paper width: {Width}", paperWidth);
            throw new ArgumentException("Paper width must be positive", nameof(paperWidth));
        }

        var availableWidth = paperWidth - margins.TotalHorizontal;

        if (availableWidth <= 0)
        {
            _logger.Error("Margins too large: Available width is {Width}", availableWidth);
            throw new ArgumentException("Margins are too large for the given paper width");
        }

        _logger.Debug("Available width calculated: {Width}px", availableWidth);
        return availableWidth;
    }

    /// <summary>
    /// Calculates the effective writing area height given paper height and margins
    /// </summary>
    public double CalculateAvailableHeight(double paperHeight, DocumentMargins margins)
    {
        _logger.Debug("Calculating available height: PaperHeight={PaperHeight}, Margins={Margins}",
            paperHeight, margins);

        if (paperHeight <= 0)
        {
            _logger.Error("Invalid paper height: {Height}", paperHeight);
            throw new ArgumentException("Paper height must be positive", nameof(paperHeight));
        }

        var availableHeight = paperHeight - margins.TotalVertical;

        if (availableHeight <= 0)
        {
            _logger.Error("Margins too large: Available height is {Height}", availableHeight);
            throw new ArgumentException("Margins are too large for the given paper height");
        }

        _logger.Debug("Available height calculated: {Height}px", availableHeight);
        return availableHeight;
    }

    /// <summary>
    /// Validates that margins are within acceptable bounds for the paper size
    /// </summary>
    public bool ValidateMargins(double paperWidth, double paperHeight, DocumentMargins margins)
    {
        _logger.Debug("Validating margins for paper {Width}x{Height}, margins: {Margins}",
            paperWidth, paperHeight, margins);

        try
        {
            CalculateAvailableWidth(paperWidth, margins);
            CalculateAvailableHeight(paperHeight, margins);
            _logger.Information("Margins validated successfully");
            return true;
        }
        catch (ArgumentException ex)
        {
            _logger.Warning(ex, "Margin validation failed");
            return false;
        }
    }

    /// <summary>
    /// Converts millimeters to pixels at 96 DPI
    /// </summary>
    public double MillimetersToPixels(double millimeters)
    {
        const double PIXELS_PER_MM = 96.0 / 25.4; // 96 DPI / 25.4mm per inch
        return millimeters * PIXELS_PER_MM;
    }

    /// <summary>
    /// Converts pixels to millimeters at 96 DPI
    /// </summary>
    public double PixelsToMillimeters(double pixels)
    {
        const double MM_PER_PIXEL = 25.4 / 96.0;
        return pixels * MM_PER_PIXEL;
    }
}
