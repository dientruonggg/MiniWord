using MiniWord.Core.Models;
using MiniWord.Core.Services;
using Xunit;

namespace MiniWord.Tests.Services;

public class DocumentManagerTests
{
    [Fact]
    public void DocumentManager_Constructor_CreatesNewDocument()
    {
        // Act
        var manager = new DocumentManager();

        // Assert
        Assert.NotNull(manager.Document);
        Assert.Empty(manager.Document.Paragraphs);
    }

    [Fact]
    public void DocumentManager_ConstructorWithDocument_UsesProvidedDocument()
    {
        // Arrange
        var document = new Document { Title = "Test Doc" };

        // Act
        var manager = new DocumentManager(document);

        // Assert
        Assert.Equal("Test Doc", manager.Document.Title);
    }

    [Fact]
    public void DocumentManager_CreateNewDocument_ReplacesCurrentDocument()
    {
        // Arrange
        var manager = new DocumentManager();
        manager.AppendParagraph("Old content");

        // Act
        manager.CreateNewDocument("New Document");

        // Assert
        Assert.Equal("New Document", manager.Document.Title);
        Assert.Empty(manager.Document.Paragraphs);
    }

    [Fact]
    public void DocumentManager_AppendParagraph_AddsToEnd()
    {
        // Arrange
        var manager = new DocumentManager();

        // Act
        manager.AppendParagraph("First");
        manager.AppendParagraph("Second");

        // Assert
        Assert.Equal(2, manager.GetParagraphCount());
        Assert.Equal("First", manager.Document.Paragraphs[0].GetText());
        Assert.Equal("Second", manager.Document.Paragraphs[1].GetText());
    }

    [Fact]
    public void DocumentManager_InsertText_CreatesParasAsNeeded()
    {
        // Arrange
        var manager = new DocumentManager();

        // Act
        manager.InsertText(2, "Hello"); // Index 2, should create 3 paragraphs

        // Assert
        Assert.Equal(3, manager.GetParagraphCount());
        Assert.Equal("Hello", manager.Document.Paragraphs[2].GetText());
    }

    [Fact]
    public void DocumentManager_InsertParagraphAt_InsertsAtCorrectPosition()
    {
        // Arrange
        var manager = new DocumentManager();
        manager.AppendParagraph("First");
        manager.AppendParagraph("Third");

        // Act
        manager.InsertParagraphAt(1, "Second");

        // Assert
        Assert.Equal(3, manager.GetParagraphCount());
        Assert.Equal("First", manager.Document.Paragraphs[0].GetText());
        Assert.Equal("Second", manager.Document.Paragraphs[1].GetText());
        Assert.Equal("Third", manager.Document.Paragraphs[2].GetText());
    }

    [Fact]
    public void DocumentManager_RemoveParagraphAt_RemovesCorrectParagraph()
    {
        // Arrange
        var manager = new DocumentManager();
        manager.AppendParagraph("First");
        manager.AppendParagraph("Second");
        manager.AppendParagraph("Third");

        // Act
        manager.RemoveParagraphAt(1);

        // Assert
        Assert.Equal(2, manager.GetParagraphCount());
        Assert.Equal("First", manager.Document.Paragraphs[0].GetText());
        Assert.Equal("Third", manager.Document.Paragraphs[1].GetText());
    }

    [Fact]
    public void DocumentManager_MergeParagraphs_CombinesTwoParagraphs()
    {
        // Arrange
        var manager = new DocumentManager();
        manager.AppendParagraph("Hello ");
        manager.AppendParagraph("World");

        // Act
        manager.MergeParagraphs(0);

        // Assert
        Assert.Equal(1, manager.GetParagraphCount());
        Assert.Equal("Hello World", manager.Document.Paragraphs[0].GetText());
    }

    [Fact]
    public void DocumentManager_SplitParagraph_CreatesTwoParagraphs()
    {
        // Arrange
        var manager = new DocumentManager();
        var paragraph = new Paragraph();
        paragraph.AddRun(new TextRun { Text = "Hello " });
        paragraph.AddRun(new TextRun { Text = "World" });
        manager.Document.AddParagraph(paragraph);

        // Act
        manager.SplitParagraph(0, 1); // Split after first run

        // Assert
        Assert.Equal(2, manager.GetParagraphCount());
        Assert.Equal("Hello ", manager.Document.Paragraphs[0].GetText());
        Assert.Equal("World", manager.Document.Paragraphs[1].GetText());
    }

    [Fact]
    public void DocumentManager_ClearDocument_RemovesAllContent()
    {
        // Arrange
        var manager = new DocumentManager();
        manager.AppendParagraph("Content 1");
        manager.AppendParagraph("Content 2");

        // Act
        manager.ClearDocument();

        // Assert
        Assert.Equal(0, manager.GetParagraphCount());
    }

    [Fact]
    public void DocumentManager_GetCharacterCount_ReturnsCorrectCount()
    {
        // Arrange
        var manager = new DocumentManager();
        manager.AppendParagraph("Hello"); // 5 chars
        manager.AppendParagraph("World"); // 5 chars

        // Act
        var count = manager.GetCharacterCount();

        // Assert
        Assert.Equal(10, count);
    }

    [Fact]
    public void DocumentManager_GetWordCount_ReturnsCorrectCount()
    {
        // Arrange
        var manager = new DocumentManager();
        manager.AppendParagraph("Hello World"); // 2 words
        manager.AppendParagraph("Test"); // 1 word

        // Act
        var count = manager.GetWordCount();

        // Assert
        Assert.Equal(3, count);
    }

    [Fact]
    public void DocumentManager_InsertText_WithFormatting_AppliesFormatting()
    {
        // Arrange
        var manager = new DocumentManager();
        var formatting = new TextRun
        {
            FontFamily = "Arial",
            FontSize = 14,
            IsBold = true
        };

        // Act
        manager.InsertText(0, "Formatted Text", formatting);

        // Assert
        var run = manager.Document.Paragraphs[0].Runs[0];
        Assert.Equal("Formatted Text", run.Text);
        Assert.Equal("Arial", run.FontFamily);
        Assert.Equal(14, run.FontSize);
        Assert.True(run.IsBold);
    }
}
