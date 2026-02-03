using MiniWord.Core.Models;
using Xunit;

namespace MiniWord.Tests;

/// <summary>
/// Unit tests for text formatting functionality (P5.3)
/// </summary>
public class FormattingSpanTests
{
    [Fact]
    public void FormattingSpan_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var span = new FormattingSpan();

        // Assert
        Assert.Equal(0, span.StartIndex);
        Assert.Equal(0, span.Length);
        Assert.Equal(TextFormatting.None, span.Formatting);
        Assert.Equal(0, span.EndIndex);
    }

    [Fact]
    public void FormattingSpan_ShouldInitializeWithProvidedValues()
    {
        // Arrange & Act
        var span = new FormattingSpan(10, 5, TextFormatting.Bold);

        // Assert
        Assert.Equal(10, span.StartIndex);
        Assert.Equal(5, span.Length);
        Assert.Equal(TextFormatting.Bold, span.Formatting);
        Assert.Equal(15, span.EndIndex);
    }

    [Fact]
    public void FormattingSpan_EndIndex_ShouldReturnCorrectValue()
    {
        // Arrange
        var span = new FormattingSpan(5, 10, TextFormatting.Italic);

        // Act
        var endIndex = span.EndIndex;

        // Assert
        Assert.Equal(15, endIndex);
    }

    [Fact]
    public void FormattingSpan_OverlapsWith_ShouldDetectOverlap()
    {
        // Arrange
        var span1 = new FormattingSpan(0, 10, TextFormatting.Bold); // [0, 10)
        var span2 = new FormattingSpan(5, 10, TextFormatting.Italic); // [5, 15)

        // Act & Assert
        Assert.True(span1.OverlapsWith(span2));
        Assert.True(span2.OverlapsWith(span1));
    }

    [Fact]
    public void FormattingSpan_OverlapsWith_ShouldNotDetectNonOverlap()
    {
        // Arrange
        var span1 = new FormattingSpan(0, 5, TextFormatting.Bold); // [0, 5)
        var span2 = new FormattingSpan(10, 5, TextFormatting.Italic); // [10, 15)

        // Act & Assert
        Assert.False(span1.OverlapsWith(span2));
        Assert.False(span2.OverlapsWith(span1));
    }

    [Fact]
    public void FormattingSpan_OverlapsWith_ShouldDetectAdjacentAsNonOverlap()
    {
        // Arrange
        var span1 = new FormattingSpan(0, 5, TextFormatting.Bold); // [0, 5)
        var span2 = new FormattingSpan(5, 5, TextFormatting.Italic); // [5, 10)

        // Act & Assert
        Assert.False(span1.OverlapsWith(span2));
        Assert.False(span2.OverlapsWith(span1));
    }

    [Fact]
    public void FormattingSpan_Contains_ShouldReturnTrueForIndexInRange()
    {
        // Arrange
        var span = new FormattingSpan(5, 10, TextFormatting.Bold); // [5, 15)

        // Act & Assert
        Assert.True(span.Contains(5));
        Assert.True(span.Contains(10));
        Assert.True(span.Contains(14));
    }

    [Fact]
    public void FormattingSpan_Contains_ShouldReturnFalseForIndexOutOfRange()
    {
        // Arrange
        var span = new FormattingSpan(5, 10, TextFormatting.Bold); // [5, 15)

        // Act & Assert
        Assert.False(span.Contains(4));
        Assert.False(span.Contains(15));
        Assert.False(span.Contains(20));
    }

    [Fact]
    public void TextFormatting_Bold_ShouldHaveCorrectFlag()
    {
        // Arrange & Act
        var formatting = TextFormatting.Bold;

        // Assert
        Assert.True(formatting.HasFlag(TextFormatting.Bold));
        Assert.False(formatting.HasFlag(TextFormatting.Italic));
        Assert.False(formatting.HasFlag(TextFormatting.Underline));
    }

    [Fact]
    public void TextFormatting_ShouldSupportMultipleFlags()
    {
        // Arrange & Act
        var formatting = TextFormatting.Bold | TextFormatting.Italic;

        // Assert
        Assert.True(formatting.HasFlag(TextFormatting.Bold));
        Assert.True(formatting.HasFlag(TextFormatting.Italic));
        Assert.False(formatting.HasFlag(TextFormatting.Underline));
    }

    [Fact]
    public void TextFormatting_ShouldSupportAllFlags()
    {
        // Arrange & Act
        var formatting = TextFormatting.Bold | TextFormatting.Italic | TextFormatting.Underline;

        // Assert
        Assert.True(formatting.HasFlag(TextFormatting.Bold));
        Assert.True(formatting.HasFlag(TextFormatting.Italic));
        Assert.True(formatting.HasFlag(TextFormatting.Underline));
    }

    [Fact]
    public void TextFormatting_ShouldSupportFlagRemoval()
    {
        // Arrange
        var formatting = TextFormatting.Bold | TextFormatting.Italic;

        // Act - Remove bold
        var newFormatting = formatting & ~TextFormatting.Bold;

        // Assert
        Assert.False(newFormatting.HasFlag(TextFormatting.Bold));
        Assert.True(newFormatting.HasFlag(TextFormatting.Italic));
    }

    [Fact]
    public void TextLine_FormattingSpans_ShouldInitializeEmpty()
    {
        // Arrange & Act
        var textLine = new TextLine();

        // Assert
        Assert.NotNull(textLine.FormattingSpans);
        Assert.Empty(textLine.FormattingSpans);
    }

    [Fact]
    public void TextLine_FormattingSpans_ShouldAllowAddingSpans()
    {
        // Arrange
        var textLine = new TextLine("Hello World", 0, 100);
        var span1 = new FormattingSpan(0, 5, TextFormatting.Bold);
        var span2 = new FormattingSpan(6, 5, TextFormatting.Italic);

        // Act
        textLine.FormattingSpans.Add(span1);
        textLine.FormattingSpans.Add(span2);

        // Assert
        Assert.Equal(2, textLine.FormattingSpans.Count);
        Assert.Equal(span1, textLine.FormattingSpans[0]);
        Assert.Equal(span2, textLine.FormattingSpans[1]);
    }

    [Fact]
    public void A4Document_FormattingSpans_ShouldInitializeEmpty()
    {
        // Arrange & Act
        var logger = Serilog.Log.Logger;
        var document = new A4Document(logger);

        // Assert
        Assert.NotNull(document.FormattingSpans);
        Assert.Empty(document.FormattingSpans);
    }

    [Fact]
    public void A4Document_FormattingSpans_ShouldAllowAddingSpans()
    {
        // Arrange
        var logger = Serilog.Log.Logger;
        var document = new A4Document(logger);
        var span = new FormattingSpan(0, 10, TextFormatting.Bold);

        // Act
        document.FormattingSpans.Add(span);

        // Assert
        Assert.Single(document.FormattingSpans);
        Assert.Equal(span, document.FormattingSpans[0]);
    }
}
