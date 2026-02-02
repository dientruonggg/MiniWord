using MiniWord.Core.Services;
using MiniWord.Core.Models;
using Serilog;

namespace MiniWord.Tests;

/// <summary>
/// Integration tests for the complete text rendering pipeline (P2.2)
/// Tests the connection between TextFlowEngine output and text rendering
/// </summary>
public class TextRenderingPipelineIntegrationTests
{
    private readonly ILogger _logger;
    private readonly TextFlowEngine _textFlowEngine;

    public TextRenderingPipelineIntegrationTests()
    {
        _logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

        _textFlowEngine = new TextFlowEngine(_logger);
    }

    /// <summary>
    /// Simulates the text measurement function that TextRenderer would provide
    /// This approximates font rendering without requiring Avalonia UI context
    /// </summary>
    private double SimulateTextMeasurement(string text)
    {
        // Simulate FormattedText measurement
        // Times New Roman 12pt: approximately 7 pixels per character on average
        return text.Length * 7.0;
    }

    [Fact]
    public void Pipeline_Integration_EmptyText_ReturnsNoLines()
    {
        // Arrange
        string text = string.Empty;
        double availableWidth = 600;

        // Act - This simulates what happens in A4Canvas.RenderTextWithPipeline
        var textLines = _textFlowEngine.CalculateLineBreaks(text, availableWidth, SimulateTextMeasurement);

        // Assert
        Assert.Empty(textLines);
    }

    [Fact]
    public void Pipeline_Integration_ShortText_CreatesSingleLine()
    {
        // Arrange
        string text = "Hello World";
        double availableWidth = 600;

        // Act - Simulate TextRenderer measurement with TextFlowEngine
        var textLines = _textFlowEngine.CalculateLineBreaks(text, availableWidth, SimulateTextMeasurement);

        // Assert
        Assert.Single(textLines);
        Assert.Equal("Hello World", textLines[0].Content);
        Assert.True(textLines[0].IsHardBreak);
        Assert.True(textLines[0].Width > 0);
    }

    [Fact]
    public void Pipeline_Integration_LongText_CreatesMultipleLines()
    {
        // Arrange
        string text = "This is a much longer text that should definitely wrap across multiple lines when we have a limited width available for rendering in the document canvas.";
        double availableWidth = 300; // Narrow width to force wrapping

        // Act
        var textLines = _textFlowEngine.CalculateLineBreaks(text, availableWidth, SimulateTextMeasurement);

        // Assert
        Assert.True(textLines.Count > 1, "Long text should create multiple lines");
        
        // Verify all lines fit within available width
        foreach (var line in textLines)
        {
            Assert.True(line.Width <= availableWidth, 
                $"Line '{line.Content}' (width: {line.Width}px) exceeds available width {availableWidth}px");
        }
        
        // Last line should have hard break
        Assert.True(textLines[^1].IsHardBreak, "Last line should have hard break");
    }

    [Fact]
    public void Pipeline_Integration_TextWithParagraphs_RespectsHardBreaks()
    {
        // Arrange
        string text = "First paragraph.\n\nSecond paragraph with more text.\n\nThird paragraph.";
        double availableWidth = 600;

        // Act
        var textLines = _textFlowEngine.CalculateLineBreaks(text, availableWidth, SimulateTextMeasurement);

        // Assert
        Assert.True(textLines.Count >= 5, "Should have multiple lines including empty lines for paragraph breaks");
        
        // All lines should have hard breaks (each paragraph ends with newline)
        foreach (var line in textLines)
        {
            Assert.True(line.IsHardBreak, "Each line should preserve paragraph structure");
        }
    }

    [Fact]
    public void Pipeline_Integration_MeasurementFunction_ProducesConsistentResults()
    {
        // Arrange
        string text = "Consistent text for measurement";
        double availableWidth = 500;

        // Act - Call multiple times to ensure consistency
        var lines1 = _textFlowEngine.CalculateLineBreaks(text, availableWidth, SimulateTextMeasurement);
        var lines2 = _textFlowEngine.CalculateLineBreaks(text, availableWidth, SimulateTextMeasurement);

        // Assert
        Assert.Equal(lines1.Count, lines2.Count);
        for (int i = 0; i < lines1.Count; i++)
        {
            Assert.Equal(lines1[i].Content, lines2[i].Content);
            Assert.Equal(lines1[i].Width, lines2[i].Width);
            Assert.Equal(lines1[i].IsHardBreak, lines2[i].IsHardBreak);
        }
    }

    [Fact]
    public void Pipeline_Integration_A4Document_CalculatesAvailableWidth()
    {
        // Arrange
        var document = new A4Document(_logger);
        var defaultMargins = document.Margins; // Default 96px margins
        
        // Act - Simulate what A4Canvas does
        double availableWidth = A4Document.A4_WIDTH_PX - defaultMargins.TotalHorizontal;
        
        string text = "Sample text to flow within document margins.";
        var textLines = _textFlowEngine.CalculateLineBreaks(text, availableWidth, SimulateTextMeasurement);

        // Assert
        Assert.True(availableWidth > 0, "Available width should be positive");
        Assert.True(availableWidth < A4Document.A4_WIDTH_PX, "Available width should be less than full page width");
        Assert.NotEmpty(textLines);
        
        // All lines should fit within the calculated width
        foreach (var line in textLines)
        {
            Assert.True(line.Width <= availableWidth);
        }
    }

    [Fact]
    public void Pipeline_Integration_CustomMargins_AffectsLineBreaking()
    {
        // Arrange
        var document = new A4Document(_logger);
        
        // Wider margins = less available width = more line breaks
        var wideMargins = new DocumentMargins(left: 150, right: 150, top: 96, bottom: 96);
        document.UpdateMargins(wideMargins);
        
        double availableWidth = document.AvailableWidth;
        string text = "This text will wrap differently with different margin settings applied to the document.";

        // Act
        var linesWithWideMargins = _textFlowEngine.CalculateLineBreaks(text, availableWidth, SimulateTextMeasurement);

        // Now with narrower margins
        var narrowMargins = new DocumentMargins(left: 50, right: 50, top: 96, bottom: 96);
        document.UpdateMargins(narrowMargins);
        double widerAvailableWidth = document.AvailableWidth;
        var linesWithNarrowMargins = _textFlowEngine.CalculateLineBreaks(text, widerAvailableWidth, SimulateTextMeasurement);

        // Assert
        Assert.True(widerAvailableWidth > availableWidth, "Narrow margins should provide more width");
        Assert.True(linesWithNarrowMargins.Count <= linesWithWideMargins.Count, 
            "More available width should result in fewer lines");
    }

    [Fact]
    public void Pipeline_Integration_LineHeight_EstimatesPageCapacity()
    {
        // Arrange
        var document = new A4Document(_logger);
        double lineHeight = 14.4; // 12px font * 1.2 line spacing (typical)
        double availableHeight = document.AvailableHeight;

        // Act - Estimate how many lines fit on one page
        int estimatedLines = _textFlowEngine.EstimateLinesInHeight(availableHeight, lineHeight);

        // Assert
        Assert.True(estimatedLines > 0, "Should fit at least one line");
        Assert.True(estimatedLines < 200, "Should have reasonable line count per page");
        
        // Verify calculation
        double totalHeight = estimatedLines * lineHeight;
        Assert.True(totalHeight <= availableHeight, "Total height should fit within available height");
    }

    [Fact]
    public void Pipeline_Integration_CompleteWorkflow_SimulatesA4CanvasRendering()
    {
        // This test simulates the complete workflow that happens in A4Canvas.RenderTextWithPipeline()
        
        // Arrange - Setup like A4Canvas would
        var document = new A4Document(_logger);
        double availableWidth = document.AvailableWidth;
        double lineHeight = 14.4; // TextRenderer would calculate this
        
        string sampleText = @"MiniWord Text Rendering Pipeline Integration Test

This is a demonstration of the complete text rendering pipeline implemented in Phase 2.2.

The pipeline consists of:
1. TextRenderer - Provides accurate text measurement using Avalonia's FormattedText API
2. TextFlowEngine - Calculates line breaks based on available width
3. TextRenderVisual - Renders the text lines with proper baseline alignment

This integration test verifies that all components work together correctly.";

        // Act - Step 1: Calculate line breaks (TextFlowEngine + TextRenderer measurement)
        var textLines = _textFlowEngine.CalculateLineBreaks(sampleText, availableWidth, SimulateTextMeasurement);
        
        // Step 2: Calculate rendering dimensions
        int estimatedLines = textLines.Count;
        double requiredHeight = estimatedLines * lineHeight;
        
        // Step 3: Check if content fits on page
        bool fitsOnOnePage = requiredHeight <= document.AvailableHeight;

        // Assert - Verify pipeline produced valid output
        Assert.NotEmpty(textLines);
        Assert.True(estimatedLines > 5, "Sample text should produce multiple lines");
        
        // Verify text is properly broken into lines
        string reconstructed = string.Join("\n", textLines.Select(l => l.Content));
        Assert.Contains("MiniWord", reconstructed);
        Assert.Contains("TextRenderer", reconstructed);
        
        // Verify all lines respect width constraints
        foreach (var line in textLines)
        {
            Assert.True(line.Width <= availableWidth);
        }
        
        _logger.Information("Pipeline integration test complete: {LineCount} lines, {Height}px height, fits on page: {FitsOnPage}",
            estimatedLines, requiredHeight, fitsOnOnePage);
    }
}
