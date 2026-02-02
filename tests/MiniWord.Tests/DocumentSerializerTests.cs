using MiniWord.Core.Exceptions;
using MiniWord.Core.Models;
using MiniWord.Core.Services;
using Serilog;
using Serilog.Core;
using Xunit;

namespace MiniWord.Tests;

public class DocumentSerializerTests : IDisposable
{
    private readonly ILogger _logger;
    private readonly DocumentSerializer _serializer;
    private readonly string _tempDirectory;
    private readonly List<string> _filesToCleanup;

    public DocumentSerializerTests()
    {
        _logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

        _serializer = new DocumentSerializer(_logger);
        _tempDirectory = Path.Combine(Path.GetTempPath(), "miniword-tests", Guid.NewGuid().ToString());
        _filesToCleanup = new List<string>();
        
        // Ensure temp directory exists
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        // Cleanup temp files
        foreach (var file in _filesToCleanup)
        {
            if (File.Exists(file))
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        // Cleanup temp directory
        if (Directory.Exists(_tempDirectory))
        {
            try
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    private string GetTempFilePath()
    {
        var filePath = Path.Combine(_tempDirectory, $"test-{Guid.NewGuid()}.miniword");
        _filesToCleanup.Add(filePath);
        return filePath;
    }

    [Fact]
    public async Task SerializeAsync_WithDefaultMargins_Success()
    {
        // Arrange
        var document = new A4Document(_logger)
        {
            Content = "Test content"
        };
        var filePath = GetTempFilePath();

        // Act
        await _serializer.SerializeAsync(document, filePath);

        // Assert
        Assert.True(File.Exists(filePath));
        var fileContent = await File.ReadAllTextAsync(filePath);
        Assert.Contains("Test content", fileContent);
        Assert.Contains("Margins", fileContent);
    }

    [Fact]
    public async Task SerializeAsync_WithCustomMargins_Success()
    {
        // Arrange
        var document = new A4Document(_logger)
        {
            Content = "Custom margins test"
        };
        var customMargins = new DocumentMargins(50, 50, 75, 75);
        document.UpdateMargins(customMargins);
        var filePath = GetTempFilePath();

        // Act
        await _serializer.SerializeAsync(document, filePath);

        // Assert
        Assert.True(File.Exists(filePath));
        var fileContent = await File.ReadAllTextAsync(filePath);
        Assert.Contains("50", fileContent); // Check for custom margin values
        Assert.Contains("75", fileContent);
    }

    [Fact]
    public async Task SerializeAsync_WithMultiplePages_Success()
    {
        // Arrange
        var document = new A4Document(_logger);
        document.AddPage("Page 1 content");
        document.AddPage("Page 2 content");
        document.AddPage("Page 3 content");
        var filePath = GetTempFilePath();

        // Act
        await _serializer.SerializeAsync(document, filePath);

        // Assert
        Assert.True(File.Exists(filePath));
        var fileContent = await File.ReadAllTextAsync(filePath);
        Assert.Contains("Page 1 content", fileContent);
        Assert.Contains("Page 2 content", fileContent);
        Assert.Contains("Page 3 content", fileContent);
    }

    [Fact]
    public async Task SerializeAsync_NullDocument_ThrowsDocumentException()
    {
        // Arrange
        var filePath = GetTempFilePath();

        // Act & Assert
        await Assert.ThrowsAsync<DocumentException>(
            async () => await _serializer.SerializeAsync(null!, filePath));
    }

    [Fact]
    public async Task SerializeAsync_EmptyFilePath_ThrowsDocumentException()
    {
        // Arrange
        var document = new A4Document(_logger);

        // Act & Assert
        await Assert.ThrowsAsync<DocumentException>(
            async () => await _serializer.SerializeAsync(document, ""));
    }

    [Fact]
    public async Task SerializeAsync_CreatesDirectoryIfNotExists()
    {
        // Arrange
        var document = new A4Document(_logger)
        {
            Content = "Test content"
        };
        var nestedDir = Path.Combine(_tempDirectory, "nested", "path");
        var filePath = Path.Combine(nestedDir, "test.miniword");
        _filesToCleanup.Add(filePath);

        // Act
        await _serializer.SerializeAsync(document, filePath);

        // Assert
        Assert.True(Directory.Exists(nestedDir));
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public async Task DeserializeAsync_WithValidFile_Success()
    {
        // Arrange
        var originalDocument = new A4Document(_logger)
        {
            Content = "Test document content"
        };
        var filePath = GetTempFilePath();
        await _serializer.SerializeAsync(originalDocument, filePath);

        // Act
        var deserializedDocument = await _serializer.DeserializeAsync(filePath, _logger);

        // Assert
        Assert.NotNull(deserializedDocument);
        Assert.Equal(originalDocument.Content, deserializedDocument.Content);
        Assert.Equal(originalDocument.Margins.Left, deserializedDocument.Margins.Left);
        Assert.Equal(originalDocument.Margins.Right, deserializedDocument.Margins.Right);
        Assert.Equal(originalDocument.Margins.Top, deserializedDocument.Margins.Top);
        Assert.Equal(originalDocument.Margins.Bottom, deserializedDocument.Margins.Bottom);
        Assert.False(deserializedDocument.IsDirty); // Should be marked as saved after loading
    }

    [Fact]
    public async Task DeserializeAsync_WithCustomMargins_RestoresMargins()
    {
        // Arrange
        var originalDocument = new A4Document(_logger);
        var customMargins = new DocumentMargins(30, 40, 50, 60);
        originalDocument.UpdateMargins(customMargins);
        var filePath = GetTempFilePath();
        await _serializer.SerializeAsync(originalDocument, filePath);

        // Act
        var deserializedDocument = await _serializer.DeserializeAsync(filePath, _logger);

        // Assert
        Assert.Equal(30, deserializedDocument.Margins.Left);
        Assert.Equal(40, deserializedDocument.Margins.Right);
        Assert.Equal(50, deserializedDocument.Margins.Top);
        Assert.Equal(60, deserializedDocument.Margins.Bottom);
    }

    [Fact]
    public async Task DeserializeAsync_WithMultiplePages_RestoresAllPages()
    {
        // Arrange
        var originalDocument = new A4Document(_logger);
        originalDocument.AddPage("Page 1");
        originalDocument.AddPage("Page 2");
        originalDocument.AddPage("Page 3");
        var filePath = GetTempFilePath();
        await _serializer.SerializeAsync(originalDocument, filePath);

        // Act
        var deserializedDocument = await _serializer.DeserializeAsync(filePath, _logger);

        // Assert
        Assert.Equal(4, deserializedDocument.PageCount); // 3 added + 1 initial
        Assert.Equal("Page 1", deserializedDocument.Pages[1].Content);
        Assert.Equal("Page 2", deserializedDocument.Pages[2].Content);
        Assert.Equal("Page 3", deserializedDocument.Pages[3].Content);
    }

    [Fact]
    public async Task DeserializeAsync_FileNotFound_ThrowsDocumentException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_tempDirectory, "nonexistent.miniword");

        // Act & Assert
        await Assert.ThrowsAsync<DocumentException>(
            async () => await _serializer.DeserializeAsync(nonExistentPath, _logger));
    }

    [Fact]
    public async Task DeserializeAsync_EmptyFilePath_ThrowsDocumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<DocumentException>(
            async () => await _serializer.DeserializeAsync("", _logger));
    }

    [Fact]
    public async Task DeserializeAsync_InvalidJson_ThrowsDocumentException()
    {
        // Arrange
        var filePath = GetTempFilePath();
        await File.WriteAllTextAsync(filePath, "This is not valid JSON{]");

        // Act & Assert
        await Assert.ThrowsAsync<DocumentException>(
            async () => await _serializer.DeserializeAsync(filePath, _logger));
    }

    [Fact]
    public async Task RoundTrip_PreservesAllDocumentProperties()
    {
        // Arrange
        var originalDocument = new A4Document(_logger)
        {
            Content = "Round trip test content"
        };
        
        // Set custom margins
        var customMargins = new DocumentMargins(25, 35, 45, 55);
        originalDocument.UpdateMargins(customMargins);
        
        // Add pages
        originalDocument.AddPage("First page");
        originalDocument.AddPage("Second page");
        
        // Navigate to second page
        originalDocument.GoToNextPage();
        
        var filePath = GetTempFilePath();

        // Act - Serialize then deserialize
        await _serializer.SerializeAsync(originalDocument, filePath);
        var restoredDocument = await _serializer.DeserializeAsync(filePath, _logger);

        // Assert
        Assert.Equal(originalDocument.Content, restoredDocument.Content);
        Assert.Equal(originalDocument.Margins.Left, restoredDocument.Margins.Left);
        Assert.Equal(originalDocument.Margins.Right, restoredDocument.Margins.Right);
        Assert.Equal(originalDocument.Margins.Top, restoredDocument.Margins.Top);
        Assert.Equal(originalDocument.Margins.Bottom, restoredDocument.Margins.Bottom);
        Assert.Equal(originalDocument.PageCount, restoredDocument.PageCount);
        Assert.Equal(originalDocument.CurrentPageIndex, restoredDocument.CurrentPageIndex);
        Assert.False(restoredDocument.IsDirty);
    }

    [Fact]
    public async Task SerializeAsync_WithTextLines_PreservesLineData()
    {
        // Arrange
        var document = new A4Document(_logger);
        var page = document.GetCurrentPage();
        page!.Lines.Add(new TextLine("Line 1", 0, 100.0, false));
        page.Lines.Add(new TextLine("Line 2", 7, 150.0, true));
        var filePath = GetTempFilePath();

        // Act
        await _serializer.SerializeAsync(document, filePath);
        var restoredDocument = await _serializer.DeserializeAsync(filePath, _logger);

        // Assert
        var restoredPage = restoredDocument.GetCurrentPage();
        Assert.NotNull(restoredPage);
        Assert.Equal(2, restoredPage.Lines.Count);
        Assert.Equal("Line 1", restoredPage.Lines[0].Content);
        Assert.Equal(0, restoredPage.Lines[0].StartIndex);
        Assert.Equal(100.0, restoredPage.Lines[0].Width);
        Assert.False(restoredPage.Lines[0].IsHardBreak);
        Assert.Equal("Line 2", restoredPage.Lines[1].Content);
        Assert.Equal(7, restoredPage.Lines[1].StartIndex);
        Assert.Equal(150.0, restoredPage.Lines[1].Width);
        Assert.True(restoredPage.Lines[1].IsHardBreak);
    }

    [Fact]
    public async Task DeserializeAsync_MarksDocumentAsNotDirty()
    {
        // Arrange
        var document = new A4Document(_logger)
        {
            Content = "Test content"
        };
        var filePath = GetTempFilePath();
        await _serializer.SerializeAsync(document, filePath);

        // Act
        var loadedDocument = await _serializer.DeserializeAsync(filePath, _logger);

        // Assert
        Assert.False(loadedDocument.IsDirty);
    }
}
