using MiniWord.Core.Models;
using Xunit;

namespace MiniWord.Tests.Models;

public class ParagraphTests
{
    [Fact]
    public void Paragraph_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var paragraph = new Paragraph();

        // Assert
        Assert.Empty(paragraph.Runs);
        Assert.Equal(TextAlignment.Left, paragraph.Alignment);
        Assert.Equal(1.0, paragraph.LineSpacing);
        Assert.Equal(0.0, paragraph.LeftIndent);
        Assert.Equal(0.0, paragraph.RightIndent);
        Assert.Equal(0.0, paragraph.SpacingBefore);
        Assert.Equal(0.0, paragraph.SpacingAfter);
    }

    [Fact]
    public void Paragraph_AddRun_AddsRunCorrectly()
    {
        // Arrange
        var paragraph = new Paragraph();
        var run = new TextRun { Text = "Hello" };

        // Act
        paragraph.AddRun(run);

        // Assert
        Assert.Single(paragraph.Runs);
        Assert.Equal("Hello", paragraph.Runs[0].Text);
    }

    [Fact]
    public void Paragraph_GetText_ConcatenatesAllRuns()
    {
        // Arrange
        var paragraph = new Paragraph();
        paragraph.AddRun(new TextRun { Text = "Hello " });
        paragraph.AddRun(new TextRun { Text = "World" });
        paragraph.AddRun(new TextRun { Text = "!" });

        // Act
        var text = paragraph.GetText();

        // Assert
        Assert.Equal("Hello World!", text);
    }

    [Fact]
    public void Paragraph_IsEmpty_ReturnsTrueForEmptyParagraph()
    {
        // Arrange
        var paragraph = new Paragraph();

        // Act & Assert
        Assert.True(paragraph.IsEmpty());
    }

    [Fact]
    public void Paragraph_IsEmpty_ReturnsFalseForNonEmptyParagraph()
    {
        // Arrange
        var paragraph = new Paragraph();
        paragraph.AddRun(new TextRun { Text = "Content" });

        // Act & Assert
        Assert.False(paragraph.IsEmpty());
    }

    [Fact]
    public void Paragraph_IsEmpty_ReturnsTrueForEmptyTextRuns()
    {
        // Arrange
        var paragraph = new Paragraph();
        paragraph.AddRun(new TextRun { Text = "" });
        paragraph.AddRun(new TextRun { Text = "" });

        // Act & Assert
        Assert.True(paragraph.IsEmpty());
    }

    [Fact]
    public void Paragraph_SetAlignment_WorksCorrectly()
    {
        // Arrange
        var paragraph = new Paragraph();

        // Act
        paragraph.Alignment = TextAlignment.Center;

        // Assert
        Assert.Equal(TextAlignment.Center, paragraph.Alignment);
    }
}
