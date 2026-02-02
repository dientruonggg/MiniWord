using MiniWord.Core.Exceptions;
using MiniWord.Core.Models;
using Serilog;

namespace MiniWord.Core.Services;

/// <summary>
/// Service for handling text flow and wrapping logic
/// </summary>
public class TextFlowEngine
{
    private readonly ILogger _logger;

    public TextFlowEngine(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Calculates line breaks for text based on available width
    /// This is a basic implementation - in a real app, you'd use FormattedText or similar
    /// </summary>
    public List<TextLine> CalculateLineBreaks(
        string text,
        double availableWidth,
        Func<string, double> measureTextWidth)
    {
        _logger.Information("Calculating line breaks for text (length: {Length}), available width: {Width}px",
            text?.Length ?? 0, availableWidth);

        if (string.IsNullOrEmpty(text))
        {
            _logger.Debug("Empty text provided, returning empty list");
            return new List<TextLine>();
        }

        if (availableWidth <= 0)
        {
            var ex = new DocumentException(
                $"Available width must be positive. Provided: {availableWidth}px",
                "INVALID_WIDTH");
            _logger.Error(ex, "Invalid available width: {Width}", availableWidth);
            throw ex;
        }

        if (measureTextWidth == null)
        {
            var ex = new DocumentException(
                "Text measurement function cannot be null",
                "NULL_MEASUREMENT_FUNCTION");
            _logger.Error(ex, "measureTextWidth is null");
            throw ex;
        }

        var lines = new List<TextLine>();
        var paragraphs = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        int currentIndex = 0;

        try
        {
            foreach (var paragraph in paragraphs)
            {
                if (string.IsNullOrEmpty(paragraph))
                {
                    // Empty line (hard break)
                    lines.Add(new TextLine(string.Empty, currentIndex, 0, true));
                    currentIndex += paragraph.Length + 1; // +1 for newline
                    continue;
                }

                // Process paragraph with word wrapping
                var words = paragraph.Split(' ');
                var currentLine = new List<string>();
                double currentLineWidth = 0;
                int lineStartIndex = currentIndex;

                foreach (var word in words)
                {
                    var testLine = string.Join(" ", currentLine.Concat(new[] { word }));
                    var testWidth = measureTextWidth(testLine);

                    if (testWidth <= availableWidth || currentLine.Count == 0)
                    {
                        // Word fits, add to current line
                        currentLine.Add(word);
                        currentLineWidth = testWidth;
                    }
                    else
                    {
                        // Word doesn't fit, save current line and start new one
                        var lineContent = string.Join(" ", currentLine);
                        lines.Add(new TextLine(lineContent, lineStartIndex, currentLineWidth, false));
                        _logger.Debug("Line added: {Line}", lineContent);

                        lineStartIndex += lineContent.Length + 1; // +1 for space
                        currentLine = new List<string> { word };
                        currentLineWidth = measureTextWidth(word);
                    }
                }

                // Add remaining line with hard break (end of paragraph)
                if (currentLine.Count > 0)
                {
                    var lineContent = string.Join(" ", currentLine);
                    lines.Add(new TextLine(lineContent, lineStartIndex, currentLineWidth, true));
                    _logger.Debug("Line added (hard break): {Line}", lineContent);
                }

                currentIndex += paragraph.Length + 1; // +1 for newline
            }

            _logger.Information("Line breaks calculated: {LineCount} lines generated", lines.Count);
            return lines;
        }
        catch (Exception ex) when (ex is not DocumentException)
        {
            _logger.Error(ex, "Failed to calculate line breaks");
            throw new DocumentException("Failed to calculate line breaks", "LINE_BREAK_ERROR", ex);
        }
    }

    /// <summary>
    /// Reflows text when margins change
    /// </summary>
    public List<TextLine> ReflowText(
        string text,
        double newAvailableWidth,
        Func<string, double> measureTextWidth)
    {
        _logger.Information("Reflowing text for new available width: {Width}px", newAvailableWidth);

        try
        {
            return CalculateLineBreaks(text, newAvailableWidth, measureTextWidth);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to reflow text");
            throw new DocumentException("Failed to reflow text", "REFLOW_ERROR", ex);
        }
    }

    /// <summary>
    /// Estimates the number of lines that will fit in a given height
    /// </summary>
    public int EstimateLinesInHeight(double availableHeight, double lineHeight)
    {
        if (lineHeight <= 0)
        {
            var ex = new DocumentException(
                $"Line height must be positive. Provided: {lineHeight}px",
                "INVALID_LINE_HEIGHT");
            _logger.Error(ex, "Invalid line height: {Height}", lineHeight);
            throw ex;
        }

        var lineCount = (int)(availableHeight / lineHeight);
        _logger.Debug("Estimated {Count} lines in {Height}px with line height {LineHeight}px",
            lineCount, availableHeight, lineHeight);

        return lineCount;
    }
}
