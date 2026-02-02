using MiniWord.Core.Models;
using Xunit;

namespace MiniWord.Tests.Models;

public class DocumentTests
{
    [Fact]
    public void Document_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var document = new Document();

        // Assert
        Assert.Empty(document.Paragraphs);
        Assert.Equal("Untitled", document.Title);
        Assert.False(document.IsModified);
        Assert.Equal(0, document.ParagraphCount);
        Assert.Equal(0, document.CharacterCount);
        Assert.Equal(0, document.WordCount);
    }

    [Fact]
    public void Document_AddParagraph_AddsAndMarksModified()
    {
        // Arrange
        var document = new Document();
        var paragraph = new Paragraph();
        paragraph.AddRun(new TextRun { Text = "Test" });

        // Act
        document.AddParagraph(paragraph);

        // Assert
        Assert.Single(document.Paragraphs);
        Assert.True(document.IsModified);
        Assert.Equal(1, document.ParagraphCount);
    }

    [Fact]
    public void Document_InsertParagraph_InsertsAtCorrectPosition()
    {
        // Arrange
        var document = new Document();
        var para1 = new Paragraph();
        para1.AddRun(new TextRun { Text = "First" });
        var para2 = new Paragraph();
        para2.AddRun(new TextRun { Text = "Third" });
        document.AddParagraph(para1);
        document.AddParagraph(para2);

        var paraMiddle = new Paragraph();
        paraMiddle.AddRun(new TextRun { Text = "Second" });

        // Act
        document.InsertParagraph(1, paraMiddle);

        // Assert
        Assert.Equal(3, document.ParagraphCount);
        Assert.Equal("First", document.Paragraphs[0].GetText());
        Assert.Equal("Second", document.Paragraphs[1].GetText());
        Assert.Equal("Third", document.Paragraphs[2].GetText());
    }

    [Fact]
    public void Document_InsertParagraph_ThrowsOnInvalidIndex()
    {
        // Arrange
        var document = new Document();
        var paragraph = new Paragraph();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => document.InsertParagraph(-1, paragraph));
        Assert.Throws<ArgumentOutOfRangeException>(() => document.InsertParagraph(10, paragraph));
    }

    [Fact]
    public void Document_RemoveParagraph_RemovesAndMarksModified()
    {
        // Arrange
        var document = new Document();
        document.AddParagraph(new Paragraph());
        document.AddParagraph(new Paragraph());
        document.MarkAsSaved();

        // Act
        document.RemoveParagraph(0);

        // Assert
        Assert.Single(document.Paragraphs);
        Assert.True(document.IsModified);
    }

    [Fact]
    public void Document_RemoveParagraph_ThrowsOnInvalidIndex()
    {
        // Arrange
        var document = new Document();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => document.RemoveParagraph(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => document.RemoveParagraph(-1));
    }

    [Fact]
    public void Document_CharacterCount_CalculatesCorrectly()
    {
        // Arrange
        var document = new Document();
        var para1 = new Paragraph();
        para1.AddRun(new TextRun { Text = "Hello" }); // 5 chars
        var para2 = new Paragraph();
        para2.AddRun(new TextRun { Text = "World" }); // 5 chars
        document.AddParagraph(para1);
        document.AddParagraph(para2);

        // Act & Assert
        Assert.Equal(10, document.CharacterCount);
    }

    [Fact]
    public void Document_WordCount_CalculatesCorrectly()
    {
        // Arrange
        var document = new Document();
        var para1 = new Paragraph();
        para1.AddRun(new TextRun { Text = "Hello World" }); // 2 words
        var para2 = new Paragraph();
        para2.AddRun(new TextRun { Text = "Test Document" }); // 2 words
        document.AddParagraph(para1);
        document.AddParagraph(para2);

        // Act & Assert
        Assert.Equal(4, document.WordCount);
    }

    [Fact]
    public void Document_Clear_RemovesAllParagraphs()
    {
        // Arrange
        var document = new Document();
        document.AddParagraph(new Paragraph());
        document.AddParagraph(new Paragraph());

        // Act
        document.Clear();

        // Assert
        Assert.Empty(document.Paragraphs);
        Assert.True(document.IsModified);
    }

    [Fact]
    public void Document_MarkAsSaved_ClearsModifiedFlag()
    {
        // Arrange
        var document = new Document();
        document.AddParagraph(new Paragraph());
        Assert.True(document.IsModified);

        // Act
        document.MarkAsSaved();

        // Assert
        Assert.False(document.IsModified);
    }

    [Fact]
    public void Document_MarkAsModified_UpdatesTimestamp()
    {
        // Arrange
        var document = new Document();
        var originalTime = document.LastModifiedAt;
        System.Threading.Thread.Sleep(10); // Small delay to ensure time difference

        // Act
        document.MarkAsModified();

        // Assert
        Assert.True(document.IsModified);
        Assert.True(document.LastModifiedAt > originalTime);
    }
}
