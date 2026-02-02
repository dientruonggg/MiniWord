using MiniWord.Core.Models;
using MiniWord.Core.Services;
using Serilog;
using Serilog.Core;

namespace MiniWord.Tests;

/// <summary>
/// Unit tests for MarginCalculator service
/// Demonstrates: xUnit test patterns with logging
/// </summary>
public class MarginCalculatorTests
{
    private readonly MarginCalculator _calculator;
    private readonly ILogger _logger;

    public MarginCalculatorTests()
    {
        // Setup logger for tests
        _logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

        _calculator = new MarginCalculator(_logger);
    }

    [Fact]
    public void CalculateAvailableWidth_ValidInputs_ReturnsCorrectWidth()
    {
        // Arrange
        double paperWidth = 794; // A4 width
        var margins = new DocumentMargins(96, 96, 96, 96); // 1 inch margins

        // Act
        double availableWidth = _calculator.CalculateAvailableWidth(paperWidth, margins);

        // Assert
        Assert.Equal(602, availableWidth); // 794 - 96 - 96 = 602
    }

    [Fact]
    public void CalculateAvailableWidth_MarginsToLarge_ThrowsException()
    {
        // Arrange
        double paperWidth = 794;
        var margins = new DocumentMargins(400, 400, 96, 96); // Too large

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _calculator.CalculateAvailableWidth(paperWidth, margins));
    }

    [Fact]
    public void CalculateAvailableWidth_NegativePaperWidth_ThrowsException()
    {
        // Arrange
        double paperWidth = -100;
        var margins = new DocumentMargins();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _calculator.CalculateAvailableWidth(paperWidth, margins));
    }

    [Fact]
    public void MillimetersToPixels_OneInch_Returns96Pixels()
    {
        // Arrange
        double millimeters = 25.4; // 1 inch

        // Act
        double pixels = _calculator.MillimetersToPixels(millimeters);

        // Assert
        Assert.Equal(96, pixels, precision: 1);
    }

    [Fact]
    public void PixelsToMillimeters_96Pixels_ReturnsOneInch()
    {
        // Arrange
        double pixels = 96; // 1 inch at 96 DPI

        // Act
        double millimeters = _calculator.PixelsToMillimeters(pixels);

        // Assert
        Assert.Equal(25.4, millimeters, precision: 1);
    }

    [Fact]
    public void ValidateMargins_ValidMargins_ReturnsTrue()
    {
        // Arrange
        double paperWidth = 794;
        double paperHeight = 1123;
        var margins = new DocumentMargins(50, 50, 50, 50);

        // Act
        bool isValid = _calculator.ValidateMargins(paperWidth, paperHeight, margins);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void ValidateMargins_InvalidMargins_ReturnsFalse()
    {
        // Arrange
        double paperWidth = 794;
        double paperHeight = 1123;
        var margins = new DocumentMargins(800, 50, 50, 50); // Left margin too large

        // Act
        bool isValid = _calculator.ValidateMargins(paperWidth, paperHeight, margins);

        // Assert
        Assert.False(isValid);
    }
}
