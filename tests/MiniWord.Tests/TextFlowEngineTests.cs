using MiniWord.Core.Models;
using MiniWord.Core.Services;
using MiniWord.Core.Exceptions;
using Serilog;

namespace MiniWord.Tests;

/// <summary>
/// Unit tests for TextFlowEngine service
/// Tests text wrapping, reflow logic, and exception handling
/// </summary>
public class TextFlowEngineTests
{
    private readonly TextFlowEngine _engine;
    private readonly ILogger _logger;

    public TextFlowEngineTests()
    {
        _logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

        _engine = new TextFlowEngine(_logger);
    }

    /// <summary>
    /// Simple text width measurement function for testing
    /// In real app, this would use FormattedText or similar
    /// </summary>
    private double MeasureText(string text)
    {
        // Simple approximation: each character = 8 pixels
        return text.Length * 8.0;
    }

    [Fact]
    public void CalculateLineBreaks_EmptyText_ReturnsEmptyList()
    {
        // Arrange
        string text = string.Empty;
        double availableWidth = 400;

        // Act
        var lines = _engine.CalculateLineBreaks(text, availableWidth, MeasureText);

        // Assert
        Assert.Empty(lines);
    }

    [Fact]
    public void CalculateLineBreaks_ShortText_ReturnsSingleLine()
    {
        // Arrange
        string text = "Hello World";
        double availableWidth = 400;

        // Act
        var lines = _engine.CalculateLineBreaks(text, availableWidth, MeasureText);

        // Assert
        Assert.Single(lines);
        Assert.Equal("Hello World", lines[0].Content);
        Assert.True(lines[0].IsHardBreak);
    }

    [Fact]
    public void CalculateLineBreaks_LongText_WrapsCorrectly()
    {
        // Arrange
        string text = "This is a very long text that should wrap across multiple lines when the available width is limited";
        double availableWidth = 200; // Narrow width will force wrapping

        // Act
        var lines = _engine.CalculateLineBreaks(text, availableWidth, MeasureText);

        // Assert
        Assert.True(lines.Count > 1, "Text should wrap to multiple lines");
        
        // Verify all lines fit within width
        foreach (var line in lines)
        {
            Assert.True(line.Width <= availableWidth, 
                $"Line '{line.Content}' width {line.Width} exceeds available width {availableWidth}");
        }
    }

    [Fact]
    public void CalculateLineBreaks_TextWithNewlines_RespectsHardBreaks()
    {
        // Arrange
        string text = "Line 1\nLine 2\nLine 3";
        double availableWidth = 400;

        // Act
        var lines = _engine.CalculateLineBreaks(text, availableWidth, MeasureText);

        // Assert
        Assert.Equal(3, lines.Count);
        Assert.All(lines, line => Assert.True(line.IsHardBreak));
    }

    [Fact]
    public void CalculateLineBreaks_NegativeWidth_ThrowsException()
    {
        // Arrange
        string text = "Test text";
        double availableWidth = -100;

        // Act & Assert
        Assert.Throws<DocumentException>(() =>
            _engine.CalculateLineBreaks(text, availableWidth, MeasureText));
    }

    [Fact]
    public void CalculateLineBreaks_ZeroWidth_ThrowsException()
    {
        // Arrange
        string text = "Test text";
        double availableWidth = 0;

        // Act & Assert
        Assert.Throws<DocumentException>(() =>
            _engine.CalculateLineBreaks(text, availableWidth, MeasureText));
    }

    [Fact]
    public void ReflowText_ChangedWidth_RecalculatesLines()
    {
        // Arrange
        string text = "This is a sample text for testing reflow functionality";
        double originalWidth = 300;
        double newWidth = 150; // Narrower, should create more lines

        // Act
        var originalLines = _engine.CalculateLineBreaks(text, originalWidth, MeasureText);
        var reflowedLines = _engine.ReflowText(text, newWidth, MeasureText);

        // Assert
        Assert.True(reflowedLines.Count >= originalLines.Count,
            "Narrower width should create more or equal number of lines");
    }

    [Fact]
    public void EstimateLinesInHeight_ValidInputs_ReturnsCorrectCount()
    {
        // Arrange
        double availableHeight = 1000;
        double lineHeight = 20;

        // Act
        int lineCount = _engine.EstimateLinesInHeight(availableHeight, lineHeight);

        // Assert
        Assert.Equal(50, lineCount); // 1000 / 20 = 50
    }

    [Fact]
    public void EstimateLinesInHeight_ZeroLineHeight_ThrowsException()
    {
        // Arrange
        double availableHeight = 1000;
        double lineHeight = 0;

        // Act & Assert
        Assert.Throws<DocumentException>(() =>
            _engine.EstimateLinesInHeight(availableHeight, lineHeight));
    }
}
