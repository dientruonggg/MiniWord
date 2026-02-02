using MiniWord.Core.Services;
using Serilog;
using Xunit;

namespace MiniWord.Tests;

/// <summary>
/// Unit tests for RecentFilesManager
/// Tests MRU list management, persistence, and file validation
/// </summary>
public class RecentFilesManagerTests : IDisposable
{
    private readonly ILogger _logger;
    private readonly string _testConfigDir;
    private readonly List<string> _tempFiles;

    public RecentFilesManagerTests()
    {
        _logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

        // Create temp directory for test config files
        _testConfigDir = Path.Combine(Path.GetTempPath(), "miniword-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testConfigDir);
        
        _tempFiles = new List<string>();
    }

    public void Dispose()
    {
        // Clean up temp files
        foreach (var file in _tempFiles)
        {
            try
            {
                if (File.Exists(file))
                    File.Delete(file);
            }
            catch { /* Ignore cleanup errors */ }
        }

        // Clean up test config directory
        try
        {
            if (Directory.Exists(_testConfigDir))
                Directory.Delete(_testConfigDir, true);
        }
        catch { /* Ignore cleanup errors */ }
    }

    /// <summary>
    /// Creates a temporary test file
    /// </summary>
    private string CreateTempFile(string content = "test content")
    {
        var tempFile = Path.Combine(_testConfigDir, $"test-{Guid.NewGuid()}.miniword");
        File.WriteAllText(tempFile, content);
        _tempFiles.Add(tempFile);
        return tempFile;
    }

    [Fact]
    public void Constructor_CreatesManagerWithEmptyList()
    {
        // Arrange & Act
        var manager = new RecentFilesManager(_logger);

        // Assert
        Assert.NotNull(manager);
        Assert.NotNull(manager.RecentFiles);
        Assert.Empty(manager.RecentFiles);
    }

    [Fact]
    public void AddRecentFile_AddsFileToList()
    {
        // Arrange
        var manager = new RecentFilesManager(_logger);
        var file1 = CreateTempFile();

        // Act
        manager.AddRecentFile(file1);

        // Assert
        Assert.Single(manager.RecentFiles);
        Assert.Contains(file1, manager.RecentFiles);
    }

    [Fact]
    public void AddRecentFile_MaintainsMRUOrder()
    {
        // Arrange
        var manager = new RecentFilesManager(_logger);
        var file1 = CreateTempFile();
        var file2 = CreateTempFile();
        var file3 = CreateTempFile();

        // Act
        manager.AddRecentFile(file1);
        manager.AddRecentFile(file2);
        manager.AddRecentFile(file3);

        // Assert
        Assert.Equal(3, manager.RecentFiles.Count);
        Assert.Equal(file3, manager.RecentFiles[0]); // Most recent first
        Assert.Equal(file2, manager.RecentFiles[1]);
        Assert.Equal(file1, manager.RecentFiles[2]);
    }

    [Fact]
    public void AddRecentFile_MovesExistingFileToTop()
    {
        // Arrange
        var manager = new RecentFilesManager(_logger);
        var file1 = CreateTempFile();
        var file2 = CreateTempFile();
        var file3 = CreateTempFile();

        manager.AddRecentFile(file1);
        manager.AddRecentFile(file2);
        manager.AddRecentFile(file3);

        // Act - Re-add file1, should move to top
        manager.AddRecentFile(file1);

        // Assert
        Assert.Equal(3, manager.RecentFiles.Count); // Still 3 files, not 4
        Assert.Equal(file1, manager.RecentFiles[0]); // file1 moved to top
        Assert.Equal(file3, manager.RecentFiles[1]);
        Assert.Equal(file2, manager.RecentFiles[2]);
    }

    [Fact]
    public void AddRecentFile_RespectsMaximumLimit()
    {
        // Arrange
        var manager = new RecentFilesManager(_logger, maxRecentFiles: 3);
        var file1 = CreateTempFile();
        var file2 = CreateTempFile();
        var file3 = CreateTempFile();
        var file4 = CreateTempFile();

        // Act
        manager.AddRecentFile(file1);
        manager.AddRecentFile(file2);
        manager.AddRecentFile(file3);
        manager.AddRecentFile(file4); // Should remove file1

        // Assert
        Assert.Equal(3, manager.RecentFiles.Count);
        Assert.DoesNotContain(file1, manager.RecentFiles); // Oldest removed
        Assert.Contains(file4, manager.RecentFiles);
        Assert.Equal(file4, manager.RecentFiles[0]); // Newest is first
    }

    [Fact]
    public void AddRecentFile_IgnoresNullOrEmptyPath()
    {
        // Arrange
        var manager = new RecentFilesManager(_logger);

        // Act
        manager.AddRecentFile(null!);
        manager.AddRecentFile("");
        manager.AddRecentFile("   ");

        // Assert
        Assert.Empty(manager.RecentFiles);
    }

    [Fact]
    public void RemoveRecentFile_RemovesFileFromList()
    {
        // Arrange
        var manager = new RecentFilesManager(_logger);
        var file1 = CreateTempFile();
        var file2 = CreateTempFile();

        manager.AddRecentFile(file1);
        manager.AddRecentFile(file2);

        // Act
        manager.RemoveRecentFile(file1);

        // Assert
        Assert.Single(manager.RecentFiles);
        Assert.DoesNotContain(file1, manager.RecentFiles);
        Assert.Contains(file2, manager.RecentFiles);
    }

    [Fact]
    public void ClearRecentFiles_RemovesAllFiles()
    {
        // Arrange
        var manager = new RecentFilesManager(_logger);
        var file1 = CreateTempFile();
        var file2 = CreateTempFile();

        manager.AddRecentFile(file1);
        manager.AddRecentFile(file2);

        // Act
        manager.ClearRecentFiles();

        // Assert
        Assert.Empty(manager.RecentFiles);
    }

    [Fact]
    public void ValidateAndClean_RemovesNonExistentFiles()
    {
        // Arrange
        var manager = new RecentFilesManager(_logger);
        var file1 = CreateTempFile();
        var file2 = CreateTempFile();
        var file3 = CreateTempFile();

        manager.AddRecentFile(file1);
        manager.AddRecentFile(file2);
        manager.AddRecentFile(file3);

        // Delete file2
        File.Delete(file2);

        // Act
        manager.ValidateAndClean();

        // Assert
        Assert.Equal(2, manager.RecentFiles.Count);
        Assert.Contains(file1, manager.RecentFiles);
        Assert.DoesNotContain(file2, manager.RecentFiles);
        Assert.Contains(file3, manager.RecentFiles);
    }

    [Fact]
    public void Load_LoadsEmptyListWhenConfigDoesNotExist()
    {
        // Arrange
        var manager = new RecentFilesManager(_logger);

        // Act
        manager.Load();

        // Assert
        Assert.Empty(manager.RecentFiles);
    }

    [Fact]
    public void SaveAndLoad_PersistsRecentFiles()
    {
        // Arrange
        var file1 = CreateTempFile();
        var file2 = CreateTempFile();
        var file3 = CreateTempFile();

        // Create first manager and add files
        var manager1 = new RecentFilesManager(_logger);
        manager1.AddRecentFile(file1);
        manager1.AddRecentFile(file2);
        manager1.AddRecentFile(file3);
        manager1.Save();

        // Act - Create new manager and load
        var manager2 = new RecentFilesManager(_logger);
        manager2.Load();

        // Assert
        Assert.Equal(3, manager2.RecentFiles.Count);
        Assert.Equal(file3, manager2.RecentFiles[0]); // MRU order preserved
        Assert.Equal(file2, manager2.RecentFiles[1]);
        Assert.Equal(file1, manager2.RecentFiles[2]);
    }

    [Fact]
    public void Load_FiltersOutNonExistentFiles()
    {
        // Arrange
        var file1 = CreateTempFile();
        var file2 = CreateTempFile();
        var file3 = CreateTempFile();

        // Create first manager and add files
        var manager1 = new RecentFilesManager(_logger);
        manager1.AddRecentFile(file1);
        manager1.AddRecentFile(file2);
        manager1.AddRecentFile(file3);
        manager1.Save();

        // Delete file2
        File.Delete(file2);

        // Act - Create new manager and load
        var manager2 = new RecentFilesManager(_logger);
        manager2.Load();

        // Assert
        Assert.Equal(2, manager2.RecentFiles.Count);
        Assert.Contains(file1, manager2.RecentFiles);
        Assert.DoesNotContain(file2, manager2.RecentFiles); // Non-existent file filtered out
        Assert.Contains(file3, manager2.RecentFiles);
    }

    [Fact]
    public void RecentFiles_ReturnsReadOnlyList()
    {
        // Arrange
        var manager = new RecentFilesManager(_logger);
        var file1 = CreateTempFile();

        manager.AddRecentFile(file1);

        // Act
        var recentFiles = manager.RecentFiles;

        // Assert
        Assert.IsAssignableFrom<IReadOnlyList<string>>(recentFiles);
    }
}
