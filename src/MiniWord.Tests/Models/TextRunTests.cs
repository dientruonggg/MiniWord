using MiniWord.Core.Models;
using Xunit;

namespace MiniWord.Tests.Models;

public class TextRunTests
{
    [Fact]
    public void TextRun_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var textRun = new TextRun();

        // Assert
        Assert.Equal(string.Empty, textRun.Text);
        Assert.Equal("Segoe UI", textRun.FontFamily);
        Assert.Equal(12.0, textRun.FontSize);
        Assert.False(textRun.IsBold);
        Assert.False(textRun.IsItalic);
        Assert.False(textRun.IsUnderline);
        Assert.Equal("#000000", textRun.Color);
    }

    [Fact]
    public void TextRun_SetProperties_WorksCorrectly()
    {
        // Arrange
        var textRun = new TextRun();

        // Act
        textRun.Text = "Hello World";
        textRun.FontFamily = "Arial";
        textRun.FontSize = 14.0;
        textRun.IsBold = true;
        textRun.IsItalic = true;
        textRun.IsUnderline = true;
        textRun.Color = "#FF0000";

        // Assert
        Assert.Equal("Hello World", textRun.Text);
        Assert.Equal("Arial", textRun.FontFamily);
        Assert.Equal(14.0, textRun.FontSize);
        Assert.True(textRun.IsBold);
        Assert.True(textRun.IsItalic);
        Assert.True(textRun.IsUnderline);
        Assert.Equal("#FF0000", textRun.Color);
    }

    [Fact]
    public void TextRun_Clone_CreatesExactCopy()
    {
        // Arrange
        var original = new TextRun
        {
            Text = "Test",
            FontFamily = "Times New Roman",
            FontSize = 16.0,
            IsBold = true,
            IsItalic = false,
            IsUnderline = true,
            Color = "#0000FF"
        };

        // Act
        var clone = original.Clone();

        // Assert
        Assert.Equal(original.Text, clone.Text);
        Assert.Equal(original.FontFamily, clone.FontFamily);
        Assert.Equal(original.FontSize, clone.FontSize);
        Assert.Equal(original.IsBold, clone.IsBold);
        Assert.Equal(original.IsItalic, clone.IsItalic);
        Assert.Equal(original.IsUnderline, clone.IsUnderline);
        Assert.Equal(original.Color, clone.Color);
        
        // Ensure it's a different instance
        Assert.NotSame(original, clone);
    }
}
