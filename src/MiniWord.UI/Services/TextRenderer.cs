using Avalonia;
using Avalonia.Media;
using MiniWord.Core.Models;
using Serilog;
using System;
using System.Collections.Generic;

namespace MiniWord.UI.Services;

/// <summary>
/// Service for rendering text lines using Avalonia's FormattedText API
/// Connects TextFlowEngine output with visual rendering
/// </summary>
public class TextRenderer
{
    private readonly ILogger _logger;
    private Typeface _typeface;
    private double _fontSize;
    private double _lineSpacing;

    /// <summary>
    /// Default line spacing multiplier (1.2 = 120% of font size)
    /// </summary>
    public const double DEFAULT_LINE_SPACING = 1.2;

    /// <summary>
    /// Gets the current line height in pixels
    /// </summary>
    public double LineHeight { get; private set; }

    public TextRenderer(ILogger logger, FontFamily? fontFamily = null, double fontSize = 12, double lineSpacing = DEFAULT_LINE_SPACING)
    {
        _logger = logger;
        _typeface = new Typeface(fontFamily ?? FontFamily.Default);
        _fontSize = fontSize;
        _lineSpacing = lineSpacing;
        
        CalculateLineHeight();
        
        _logger.Information("TextRenderer initialized with font size {FontSize}px, line spacing {LineSpacing}, line height {LineHeight}px",
            _fontSize, _lineSpacing, LineHeight);
    }

    /// <summary>
    /// Calculates line height based on font metrics
    /// Line height = (Ascent + Descent + LineGap) * lineSpacing
    /// </summary>
    private void CalculateLineHeight()
    {
        // Create a sample FormattedText to get font metrics
        var formattedText = new FormattedText(
            "Ag", // Text with both ascender and descender
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            _typeface,
            _fontSize,
            Brushes.Black);

        // Get font metrics from FormattedText
        // In Avalonia, Height includes the total line height
        var baseHeight = formattedText.Height;
        
        // Apply line spacing multiplier
        LineHeight = baseHeight * _lineSpacing;
        
        _logger.Debug("Calculated line height: base={BaseHeight}px, with spacing={LineHeight}px",
            baseHeight, LineHeight);
    }

    /// <summary>
    /// Measures the width of text using FormattedText
    /// This provides accurate text measurement for TextFlowEngine
    /// </summary>
    public double MeasureTextWidth(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        var formattedText = new FormattedText(
            text,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            _typeface,
            _fontSize,
            Brushes.Black);

        return formattedText.Width;
    }

    /// <summary>
    /// Gets a measurement function that can be passed to TextFlowEngine
    /// </summary>
    public Func<string, double> GetMeasurementFunction()
    {
        return MeasureTextWidth;
    }

    /// <summary>
    /// Renders a single text line at the specified position
    /// </summary>
    /// <param name="context">Drawing context</param>
    /// <param name="textLine">Text line to render</param>
    /// <param name="x">X position (left edge)</param>
    /// <param name="y">Y position (baseline)</param>
    /// <param name="foreground">Text color brush</param>
    public void RenderTextLine(DrawingContext context, TextLine textLine, double x, double y, IBrush? foreground = null)
    {
        if (string.IsNullOrEmpty(textLine.Content))
        {
            _logger.Debug("Skipping empty line at position ({X}, {Y})", x, y);
            return;
        }

        var formattedText = new FormattedText(
            textLine.Content,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            _typeface,
            _fontSize,
            foreground ?? Brushes.Black);

        // Draw text at baseline position
        var origin = new Point(x, y);
        context.DrawText(formattedText, origin);

        _logger.Debug("Rendered line at position ({X}, {Y}), content length: {Length}", 
            x, y, textLine.Content?.Length ?? 0);
    }

    /// <summary>
    /// Renders multiple text lines with automatic line spacing
    /// </summary>
    /// <param name="context">Drawing context</param>
    /// <param name="textLines">List of text lines to render</param>
    /// <param name="startX">Starting X position (left margin)</param>
    /// <param name="startY">Starting Y position (top margin)</param>
    /// <param name="foreground">Text color brush</param>
    public void RenderTextLines(DrawingContext context, List<TextLine> textLines, double startX, double startY, IBrush? foreground = null)
    {
        if (textLines == null || textLines.Count == 0)
        {
            _logger.Debug("No lines to render");
            return;
        }

        _logger.Information("Rendering {LineCount} text lines starting at ({X}, {Y})", 
            textLines.Count, startX, startY);

        double currentY = startY;

        foreach (var line in textLines)
        {
            // Calculate baseline position
            // For Avalonia FormattedText, the Y position is already at the baseline
            var baselineY = currentY;

            RenderTextLine(context, line, startX, baselineY, foreground);

            // Move to next line position
            currentY += LineHeight;
        }

        _logger.Debug("Finished rendering {LineCount} lines, final Y position: {Y}px", 
            textLines.Count, currentY);
    }

    /// <summary>
    /// Calculates the baseline position for a line at the given Y coordinate
    /// </summary>
    /// <param name="y">Top Y position</param>
    /// <returns>Baseline Y position</returns>
    public double GetBaselineY(double y)
    {
        // For Avalonia's FormattedText, we need to account for the ascent
        // to position text correctly from the top edge
        var formattedText = new FormattedText(
            "Ag",
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            _typeface,
            _fontSize,
            Brushes.Black);

        // Baseline is approximately at the ascent distance from top
        var baseline = formattedText.Baseline;
        return y + baseline;
    }

    /// <summary>
    /// Updates the font settings and recalculates metrics
    /// </summary>
    public void UpdateFont(FontFamily? fontFamily = null, double? fontSize = null, double? lineSpacing = null)
    {
        bool changed = false;

        if (fontFamily != null)
        {
            _typeface = new Typeface(fontFamily);
            changed = true;
        }

        if (fontSize.HasValue && fontSize.Value > 0)
        {
            _fontSize = fontSize.Value;
            changed = true;
        }

        if (lineSpacing.HasValue && lineSpacing.Value > 0)
        {
            _lineSpacing = lineSpacing.Value;
            changed = true;
        }

        if (changed)
        {
            CalculateLineHeight();
            _logger.Information("Font updated: size={FontSize}px, line spacing={LineSpacing}, line height={LineHeight}px",
                _fontSize, _lineSpacing, LineHeight);
        }
    }

    /// <summary>
    /// Estimates how many lines can fit in the given height
    /// </summary>
    public int EstimateLinesInHeight(double availableHeight)
    {
        if (LineHeight <= 0)
        {
            _logger.Warning("Invalid line height: {LineHeight}", LineHeight);
            return 0;
        }

        var lineCount = (int)(availableHeight / LineHeight);
        _logger.Debug("Estimated {Count} lines in {Height}px (line height: {LineHeight}px)",
            lineCount, availableHeight, LineHeight);

        return lineCount;
    }

    /// <summary>
    /// Gets the current font size
    /// </summary>
    public double FontSize => _fontSize;

    /// <summary>
    /// Gets the current line spacing multiplier
    /// </summary>
    public double LineSpacing => _lineSpacing;

    /// <summary>
    /// Gets the current typeface
    /// </summary>
    public Typeface Typeface => _typeface;
}
