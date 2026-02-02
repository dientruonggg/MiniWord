using MiniWord.Core.Models;
using Serilog;
using System.ComponentModel;

namespace MiniWord.Tests;

/// <summary>
/// Unit tests for A4Document multi-page support, INotifyPropertyChanged, and state management
/// </summary>
public class A4DocumentTests
{
    private readonly ILogger _logger;

    public A4DocumentTests()
    {
        _logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();
    }

    [Fact]
    public void Constructor_InitializesWithOnePage()
    {
        // Arrange & Act
        var document = new A4Document(_logger);

        // Assert
        Assert.Equal(1, document.PageCount);
        Assert.NotNull(document.Pages);
        Assert.Single(document.Pages);
    }

    [Fact]
    public void Constructor_InitializesCurrentPageToFirst()
    {
        // Arrange & Act
        var document = new A4Document(_logger);

        // Assert
        Assert.Equal(0, document.CurrentPageIndex);
        Assert.Equal(1, document.CurrentPageNumber);
    }

    [Fact]
    public void IsDirty_InitiallyFalse()
    {
        // Arrange & Act
        var document = new A4Document(_logger);
        document.MarkAsSaved(); // Mark initial page as saved

        // Assert
        Assert.False(document.IsDirty);
    }

    [Fact]
    public void Content_SetValue_SetsDirtyFlag()
    {
        // Arrange
        var document = new A4Document(_logger);
        document.MarkAsSaved();

        // Act
        document.Content = "Test content";

        // Assert
        Assert.True(document.IsDirty);
        Assert.Equal("Test content", document.Content);
    }

    [Fact]
    public void Content_SetSameValue_DoesNotRaisePropertyChanged()
    {
        // Arrange
        var document = new A4Document(_logger);
        document.Content = "Test";
        document.MarkAsSaved();
        
        int eventCount = 0;
        document.PropertyChanged += (s, e) => eventCount++;

        // Act
        document.Content = "Test"; // Same value

        // Assert
        Assert.Equal(0, eventCount);
        Assert.False(document.IsDirty);
    }

    [Fact]
    public void IsDirty_RaisesPropertyChanged()
    {
        // Arrange
        var document = new A4Document(_logger);
        document.MarkAsSaved();
        
        string? changedPropertyName = null;
        document.PropertyChanged += (s, e) => changedPropertyName = e.PropertyName;

        // Act
        document.IsDirty = true;

        // Assert
        Assert.Equal(nameof(document.IsDirty), changedPropertyName);
    }

    [Fact]
    public void AddPage_IncreasesPageCount()
    {
        // Arrange
        var document = new A4Document(_logger);
        document.MarkAsSaved();
        var initialCount = document.PageCount;

        // Act
        document.AddPage();

        // Assert
        Assert.Equal(initialCount + 1, document.PageCount);
        Assert.True(document.IsDirty);
    }

    [Fact]
    public void AddPage_WithContent_CreatesPageWithContent()
    {
        // Arrange
        var document = new A4Document(_logger);
        var content = "Test page content";

        // Act
        var page = document.AddPage(content);

        // Assert
        Assert.Equal(content, page.Content);
        Assert.Equal(2, page.PageNumber);
    }

    [Fact]
    public void AddPage_RaisesPropertyChangedForPageCount()
    {
        // Arrange
        var document = new A4Document(_logger);
        string? changedPropertyName = null;
        document.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(document.PageCount))
                changedPropertyName = e.PropertyName;
        };

        // Act
        document.AddPage();

        // Assert
        Assert.Equal(nameof(document.PageCount), changedPropertyName);
    }

    [Fact]
    public void RemovePage_ValidIndex_RemovesPage()
    {
        // Arrange
        var document = new A4Document(_logger);
        document.AddPage();
        document.AddPage();
        var initialCount = document.PageCount;

        // Act
        var result = document.RemovePage(1);

        // Assert
        Assert.True(result);
        Assert.Equal(initialCount - 1, document.PageCount);
    }

    [Fact]
    public void RemovePage_LastPage_ReturnsFalse()
    {
        // Arrange
        var document = new A4Document(_logger);

        // Act
        var result = document.RemovePage(0);

        // Assert
        Assert.False(result);
        Assert.Equal(1, document.PageCount);
    }

    [Fact]
    public void RemovePage_InvalidIndex_ReturnsFalse()
    {
        // Arrange
        var document = new A4Document(_logger);

        // Act
        var result = document.RemovePage(10);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void RemovePage_RenumbersRemainingPages()
    {
        // Arrange
        var document = new A4Document(_logger);
        document.AddPage();
        document.AddPage();
        document.AddPage();

        // Act
        document.RemovePage(1); // Remove page 2

        // Assert
        Assert.Equal(1, document.Pages[0].PageNumber);
        Assert.Equal(2, document.Pages[1].PageNumber);
        Assert.Equal(3, document.Pages[2].PageNumber);
    }

    [Fact]
    public void RemovePage_AdjustsCurrentPageIndex()
    {
        // Arrange
        var document = new A4Document(_logger);
        document.AddPage();
        document.AddPage();
        document.GoToLastPage();
        var lastPageIndex = document.CurrentPageIndex;

        // Act
        document.RemovePage(lastPageIndex);

        // Assert
        Assert.True(document.CurrentPageIndex < lastPageIndex);
    }

    [Fact]
    public void GetPage_ValidIndex_ReturnsPage()
    {
        // Arrange
        var document = new A4Document(_logger);
        document.AddPage();

        // Act
        var page = document.GetPage(0);

        // Assert
        Assert.NotNull(page);
        Assert.Equal(1, page.PageNumber);
    }

    [Fact]
    public void GetPage_InvalidIndex_ReturnsNull()
    {
        // Arrange
        var document = new A4Document(_logger);

        // Act
        var page = document.GetPage(10);

        // Assert
        Assert.Null(page);
    }

    [Fact]
    public void GetCurrentPage_ReturnsCurrentPage()
    {
        // Arrange
        var document = new A4Document(_logger);
        document.AddPage();
        document.GoToPage(1);

        // Act
        var page = document.GetCurrentPage();

        // Assert
        Assert.NotNull(page);
        Assert.Equal(2, page.PageNumber);
    }

    [Fact]
    public void GoToNextPage_NotAtEnd_ReturnsTrue()
    {
        // Arrange
        var document = new A4Document(_logger);
        document.AddPage();
        document.AddPage();

        // Act
        var result = document.GoToNextPage();

        // Assert
        Assert.True(result);
        Assert.Equal(1, document.CurrentPageIndex);
    }

    [Fact]
    public void GoToNextPage_AtEnd_ReturnsFalse()
    {
        // Arrange
        var document = new A4Document(_logger);
        document.GoToLastPage();

        // Act
        var result = document.GoToNextPage();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GoToPreviousPage_NotAtStart_ReturnsTrue()
    {
        // Arrange
        var document = new A4Document(_logger);
        document.AddPage();
        document.GoToPage(1);

        // Act
        var result = document.GoToPreviousPage();

        // Assert
        Assert.True(result);
        Assert.Equal(0, document.CurrentPageIndex);
    }

    [Fact]
    public void GoToPreviousPage_AtStart_ReturnsFalse()
    {
        // Arrange
        var document = new A4Document(_logger);

        // Act
        var result = document.GoToPreviousPage();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GoToPage_ValidIndex_ReturnsTrue()
    {
        // Arrange
        var document = new A4Document(_logger);
        document.AddPage();
        document.AddPage();

        // Act
        var result = document.GoToPage(2);

        // Assert
        Assert.True(result);
        Assert.Equal(2, document.CurrentPageIndex);
        Assert.Equal(3, document.CurrentPageNumber);
    }

    [Fact]
    public void GoToPage_InvalidIndex_ReturnsFalse()
    {
        // Arrange
        var document = new A4Document(_logger);

        // Act
        var result = document.GoToPage(10);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GoToPage_RaisesPropertyChanged()
    {
        // Arrange
        var document = new A4Document(_logger);
        document.AddPage();
        
        var changedProperties = new List<string>();
        document.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName!);

        // Act
        document.GoToPage(1);

        // Assert
        Assert.Contains(nameof(document.CurrentPageIndex), changedProperties);
        Assert.Contains(nameof(document.CurrentPageNumber), changedProperties);
    }

    [Fact]
    public void GoToFirstPage_NavigatesToFirstPage()
    {
        // Arrange
        var document = new A4Document(_logger);
        document.AddPage();
        document.AddPage();
        document.GoToLastPage();

        // Act
        document.GoToFirstPage();

        // Assert
        Assert.Equal(0, document.CurrentPageIndex);
        Assert.Equal(1, document.CurrentPageNumber);
    }

    [Fact]
    public void GoToLastPage_NavigatesToLastPage()
    {
        // Arrange
        var document = new A4Document(_logger);
        document.AddPage();
        document.AddPage();

        // Act
        document.GoToLastPage();

        // Assert
        Assert.Equal(2, document.CurrentPageIndex);
        Assert.Equal(3, document.CurrentPageNumber);
    }

    [Fact]
    public void ClearPages_RemovesAllPagesAndAddsOne()
    {
        // Arrange
        var document = new A4Document(_logger);
        document.AddPage();
        document.AddPage();
        document.AddPage();

        // Act
        document.ClearPages();

        // Assert
        Assert.Equal(1, document.PageCount);
        Assert.Equal(0, document.CurrentPageIndex);
        Assert.True(document.IsDirty);
    }

    [Fact]
    public void MarkAsSaved_ClearsDirtyFlag()
    {
        // Arrange
        var document = new A4Document(_logger);
        document.Content = "Test";
        Assert.True(document.IsDirty);

        // Act
        document.MarkAsSaved();

        // Assert
        Assert.False(document.IsDirty);
    }

    [Fact]
    public void PageCount_ReflectsActualPageCount()
    {
        // Arrange
        var document = new A4Document(_logger);

        // Act
        document.AddPage();
        document.AddPage();

        // Assert
        Assert.Equal(3, document.PageCount);
        Assert.Equal(document.Pages.Count, document.PageCount);
    }

    [Fact]
    public void CurrentPageNumber_IsOneBased()
    {
        // Arrange
        var document = new A4Document(_logger);

        // Act & Assert
        document.GoToPage(0);
        Assert.Equal(1, document.CurrentPageNumber);

        document.AddPage();
        document.GoToPage(1);
        Assert.Equal(2, document.CurrentPageNumber);
    }
}
