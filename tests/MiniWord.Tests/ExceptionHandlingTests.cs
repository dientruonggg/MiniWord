using MiniWord.Core.Exceptions;
using MiniWord.Core.Models;
using Serilog;

namespace MiniWord.Tests;

/// <summary>
/// Unit tests for exception handling and edge cases in Core layer
/// Tests for P1.4 - Exception handling review
/// </summary>
public class ExceptionHandlingTests
{
    private readonly ILogger _logger;

    public ExceptionHandlingTests()
    {
        _logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();
    }

    #region DocumentMargins Exception Tests

    [Fact]
    public void DocumentMargins_NegativeLeft_ThrowsMarginException()
    {
        // Act & Assert
        var ex = Assert.Throws<MarginException>(() => new DocumentMargins(-10, 50, 50, 50));
        Assert.Contains("Left margin cannot be negative", ex.Message);
    }

    [Fact]
    public void DocumentMargins_NegativeRight_ThrowsMarginException()
    {
        // Act & Assert
        var ex = Assert.Throws<MarginException>(() => new DocumentMargins(50, -10, 50, 50));
        Assert.Contains("Right margin cannot be negative", ex.Message);
    }

    [Fact]
    public void DocumentMargins_NegativeTop_ThrowsMarginException()
    {
        // Act & Assert
        var ex = Assert.Throws<MarginException>(() => new DocumentMargins(50, 50, -10, 50));
        Assert.Contains("Top margin cannot be negative", ex.Message);
    }

    [Fact]
    public void DocumentMargins_NegativeBottom_ThrowsMarginException()
    {
        // Act & Assert
        var ex = Assert.Throws<MarginException>(() => new DocumentMargins(50, 50, 50, -10));
        Assert.Contains("Bottom margin cannot be negative", ex.Message);
    }

    [Fact]
    public void DocumentMargins_ZeroMargins_IsValid()
    {
        // Act
        var margins = new DocumentMargins(0, 0, 0, 0);

        // Assert
        Assert.Equal(0, margins.Left);
        Assert.Equal(0, margins.Right);
        Assert.Equal(0, margins.Top);
        Assert.Equal(0, margins.Bottom);
    }

    #endregion

    #region A4Document Exception Tests

    [Fact]
    public void A4Document_UpdateMargins_HorizontalExceedsWidth_ThrowsMarginException()
    {
        // Arrange
        var document = new A4Document(_logger);
        var invalidMargins = new DocumentMargins(400, 400, 50, 50); // 800px total horizontal

        // Act & Assert
        var ex = Assert.Throws<MarginException>(() => document.UpdateMargins(invalidMargins));
        Assert.Contains("exceeds page width", ex.Message);
    }

    [Fact]
    public void A4Document_UpdateMargins_VerticalExceedsHeight_ThrowsMarginException()
    {
        // Arrange
        var document = new A4Document(_logger);
        var invalidMargins = new DocumentMargins(50, 50, 600, 600); // 1200px total vertical

        // Act & Assert
        var ex = Assert.Throws<MarginException>(() => document.UpdateMargins(invalidMargins));
        Assert.Contains("exceeds page height", ex.Message);
    }

    [Fact]
    public void A4Document_UpdateMargins_EdgeCase_ExactlyPageWidth_ThrowsException()
    {
        // Arrange
        var document = new A4Document(_logger);
        var invalidMargins = new DocumentMargins(397, 397, 50, 50); // Exactly 794px

        // Act & Assert
        Assert.Throws<MarginException>(() => document.UpdateMargins(invalidMargins));
    }

    #endregion

    #region Custom Exception Tests

    [Fact]
    public void DocumentException_DefaultErrorCode_IsSet()
    {
        // Arrange
        var ex = new DocumentException("Test message");

        // Assert
        Assert.Equal("DOCUMENT_ERROR", ex.ErrorCode);
        Assert.Equal("Test message", ex.Message);
    }

    [Fact]
    public void DocumentException_CustomErrorCode_IsSet()
    {
        // Arrange
        var ex = new DocumentException("Test message", "CUSTOM_ERROR");

        // Assert
        Assert.Equal("CUSTOM_ERROR", ex.ErrorCode);
        Assert.Equal("Test message", ex.Message);
    }

    [Fact]
    public void DocumentException_WithInnerException_PreservesInnerException()
    {
        // Arrange
        var innerEx = new InvalidOperationException("Inner error");
        var ex = new DocumentException("Outer error", innerEx);

        // Assert
        Assert.Equal("DOCUMENT_ERROR", ex.ErrorCode);
        Assert.Equal("Outer error", ex.Message);
        Assert.Same(innerEx, ex.InnerException);
    }

    [Fact]
    public void DocumentException_WithInnerExceptionAndCode_PreservesAll()
    {
        // Arrange
        var innerEx = new InvalidOperationException("Inner error");
        var ex = new DocumentException("Outer error", "SPECIFIC_ERROR", innerEx);

        // Assert
        Assert.Equal("SPECIFIC_ERROR", ex.ErrorCode);
        Assert.Equal("Outer error", ex.Message);
        Assert.Same(innerEx, ex.InnerException);
    }

    [Fact]
    public void MarginException_InheritsFromDocumentException()
    {
        // Arrange
        var ex = new MarginException("Margin error");

        // Assert
        Assert.IsAssignableFrom<DocumentException>(ex);
        Assert.Equal("Margin error", ex.Message);
    }

    [Fact]
    public void PageException_InheritsFromDocumentException()
    {
        // Arrange
        var ex = new PageException("Page error");

        // Assert
        Assert.IsAssignableFrom<DocumentException>(ex);
        Assert.Equal("Page error", ex.Message);
    }

    #endregion

    #region TextFlowEngine Exception Tests

    [Fact]
    public void TextFlowEngine_NullMeasureFunction_ThrowsDocumentException()
    {
        // Arrange
        var engine = new Core.Services.TextFlowEngine(_logger);

        // Act & Assert
        var ex = Assert.Throws<DocumentException>(() =>
            engine.CalculateLineBreaks("test", 100, null!));
        Assert.Contains("cannot be null", ex.Message);
        Assert.Equal("NULL_MEASUREMENT_FUNCTION", ex.ErrorCode);
    }

    [Fact]
    public void TextFlowEngine_NegativeWidth_ThrowsDocumentExceptionWithCode()
    {
        // Arrange
        var engine = new Core.Services.TextFlowEngine(_logger);

        // Act & Assert
        var ex = Assert.Throws<DocumentException>(() =>
            engine.CalculateLineBreaks("test", -50, s => s.Length * 8.0));
        Assert.Contains("must be positive", ex.Message);
        Assert.Equal("INVALID_WIDTH", ex.ErrorCode);
    }

    [Fact]
    public void TextFlowEngine_NegativeLineHeight_ThrowsDocumentExceptionWithCode()
    {
        // Arrange
        var engine = new Core.Services.TextFlowEngine(_logger);

        // Act & Assert
        var ex = Assert.Throws<DocumentException>(() =>
            engine.EstimateLinesInHeight(1000, -20));
        Assert.Contains("must be positive", ex.Message);
        Assert.Equal("INVALID_LINE_HEIGHT", ex.ErrorCode);
    }

    #endregion

    #region MarginCalculator Exception Tests

    [Fact]
    public void MarginCalculator_ZeroPaperWidth_ThrowsMarginException()
    {
        // Arrange
        var calculator = new Core.Services.MarginCalculator(_logger);
        var margins = new DocumentMargins();

        // Act & Assert
        var ex = Assert.Throws<MarginException>(() =>
            calculator.CalculateAvailableWidth(0, margins));
        Assert.Contains("must be positive", ex.Message);
    }

    [Fact]
    public void MarginCalculator_ZeroPaperHeight_ThrowsMarginException()
    {
        // Arrange
        var calculator = new Core.Services.MarginCalculator(_logger);
        var margins = new DocumentMargins();

        // Act & Assert
        var ex = Assert.Throws<MarginException>(() =>
            calculator.CalculateAvailableHeight(0, margins));
        Assert.Contains("must be positive", ex.Message);
    }

    [Fact]
    public void MarginCalculator_MarginsEqualPaperSize_ThrowsMarginException()
    {
        // Arrange
        var calculator = new Core.Services.MarginCalculator(_logger);
        var margins = new DocumentMargins(397, 397, 0, 0); // Total = 794

        // Act & Assert
        var ex = Assert.Throws<MarginException>(() =>
            calculator.CalculateAvailableWidth(794, margins));
        Assert.Contains("too large", ex.Message);
    }

    #endregion
}
