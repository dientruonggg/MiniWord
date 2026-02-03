using MiniWord.Core.Models;
using MiniWord.UI.ViewModels;
using Serilog;
using Xunit;

namespace MiniWord.Tests;

/// <summary>
/// Unit tests for FindReplaceViewModel - P5.2
/// </summary>
public class FindReplaceViewModelTests : IDisposable
{
    private readonly ILogger _logger;
    private readonly FindReplaceViewModel _viewModel;

    public FindReplaceViewModelTests()
    {
        _logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

        _viewModel = new FindReplaceViewModel();
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_InitializesViewModel_WithDefaultValues()
    {
        // Assert
        Assert.Equal(string.Empty, _viewModel.SearchText);
        Assert.Equal(string.Empty, _viewModel.ReplaceText);
        Assert.False(_viewModel.CaseSensitive);
        Assert.False(_viewModel.WholeWord);
        Assert.False(_viewModel.UseRegex);
        Assert.Equal("Ready", _viewModel.StatusText);
        Assert.False(_viewModel.HasResults);
        Assert.Equal(-1, _viewModel.CurrentResultIndex);
        Assert.Empty(_viewModel.SearchResults);
    }

    #endregion

    #region PerformSearch Tests

    [Fact]
    public void PerformSearch_WithEmptySearchText_SetsStatusAndClearsResults()
    {
        // Arrange
        var documentText = "Hello world";
        _viewModel.SearchText = "";

        // Act
        _viewModel.PerformSearch(documentText);

        // Assert
        Assert.Equal("Please enter search text", _viewModel.StatusText);
        Assert.False(_viewModel.HasResults);
        Assert.Empty(_viewModel.SearchResults);
    }

    [Fact]
    public void PerformSearch_WithEmptyDocument_SetsStatusAndClearsResults()
    {
        // Arrange
        _viewModel.SearchText = "test";

        // Act
        _viewModel.PerformSearch("");

        // Assert
        Assert.Equal("Document is empty", _viewModel.StatusText);
        Assert.False(_viewModel.HasResults);
        Assert.Empty(_viewModel.SearchResults);
    }

    [Fact]
    public void PerformSearch_WithNoMatches_ReturnsNoResults()
    {
        // Arrange
        var documentText = "Hello world";
        _viewModel.SearchText = "xyz";

        // Act
        _viewModel.PerformSearch(documentText);

        // Assert
        Assert.Equal("No matches found", _viewModel.StatusText);
        Assert.False(_viewModel.HasResults);
        Assert.Empty(_viewModel.SearchResults);
    }

    [Fact]
    public void PerformSearch_WithMatches_FindsAllOccurrences()
    {
        // Arrange
        var documentText = "Hello world, hello universe";
        _viewModel.SearchText = "hello";
        _viewModel.CaseSensitive = false;

        // Track highlight events
        var highlightedRange = default(TextRange);
        _viewModel.HighlightRequested += (s, range) => highlightedRange = range;

        // Act
        _viewModel.PerformSearch(documentText);

        // Assert
        Assert.Equal("Found 2 match(es)", _viewModel.StatusText);
        Assert.True(_viewModel.HasResults);
        Assert.Equal(2, _viewModel.SearchResults.Count);
        Assert.Equal(0, _viewModel.CurrentResultIndex);
        
        // First match should be highlighted
        Assert.NotNull(highlightedRange);
        Assert.Equal(new TextRange(0, 5), highlightedRange);
    }

    [Fact]
    public void PerformSearch_CaseSensitive_FindsExactMatches()
    {
        // Arrange
        var documentText = "Hello world, hello universe";
        _viewModel.SearchText = "hello";
        _viewModel.CaseSensitive = true;

        // Act
        _viewModel.PerformSearch(documentText);

        // Assert
        Assert.Equal("Found 1 match(es)", _viewModel.StatusText);
        Assert.True(_viewModel.HasResults);
        Assert.Single(_viewModel.SearchResults);
        Assert.Equal(new TextRange(13, 18), _viewModel.SearchResults[0]);
    }

    [Fact]
    public void PerformSearch_WholeWord_FindsCompleteWords()
    {
        // Arrange
        var documentText = "test testing tested";
        _viewModel.SearchText = "test";
        _viewModel.WholeWord = true;

        // Act
        _viewModel.PerformSearch(documentText);

        // Assert
        Assert.Equal("Found 1 match(es)", _viewModel.StatusText);
        Assert.True(_viewModel.HasResults);
        Assert.Single(_viewModel.SearchResults);
        Assert.Equal(new TextRange(0, 4), _viewModel.SearchResults[0]);
    }

    [Fact]
    public void PerformSearch_WithRegex_FindsPatternMatches()
    {
        // Arrange
        var documentText = "test123 test456 test";
        _viewModel.SearchText = @"test\d+";
        _viewModel.UseRegex = true;

        // Act
        _viewModel.PerformSearch(documentText);

        // Assert
        Assert.Equal("Found 2 match(es)", _viewModel.StatusText);
        Assert.True(_viewModel.HasResults);
        Assert.Equal(2, _viewModel.SearchResults.Count);
    }

    [Fact]
    public void PerformSearch_WithInvalidRegex_HandlesError()
    {
        // Arrange
        var documentText = "test";
        _viewModel.SearchText = "["; // Invalid regex
        _viewModel.UseRegex = true;

        // Act
        _viewModel.PerformSearch(documentText);

        // Assert
        Assert.Contains("error", _viewModel.StatusText.ToLower());
        Assert.False(_viewModel.HasResults);
        Assert.Empty(_viewModel.SearchResults);
    }

    #endregion

    #region Find Next/Previous Tests

    [Fact]
    public void FindNext_WithResults_MovesToNextMatch()
    {
        // Arrange
        var documentText = "test test test";
        _viewModel.SearchText = "test";
        _viewModel.PerformSearch(documentText);

        var highlightedRanges = new System.Collections.Generic.List<TextRange>();
        _viewModel.HighlightRequested += (s, range) => highlightedRanges.Add(range);

        // Clear the initial highlight from PerformSearch
        highlightedRanges.Clear();

        // Act
        _viewModel.FindNextCommand.Execute(null);

        // Assert
        Assert.Equal(1, _viewModel.CurrentResultIndex);
        Assert.Equal("Match 2 of 3", _viewModel.StatusText);
        Assert.Single(highlightedRanges);
        Assert.Equal(new TextRange(5, 9), highlightedRanges[0]);
    }

    [Fact]
    public void FindNext_AtLastMatch_WrapsToFirst()
    {
        // Arrange
        var documentText = "test test test";
        _viewModel.SearchText = "test";
        _viewModel.PerformSearch(documentText);

        // Move to last match
        _viewModel.FindNextCommand.Execute(null);
        _viewModel.FindNextCommand.Execute(null);
        
        var highlightedRanges = new System.Collections.Generic.List<TextRange>();
        _viewModel.HighlightRequested += (s, range) => highlightedRanges.Add(range);

        // Act - should wrap to first
        _viewModel.FindNextCommand.Execute(null);

        // Assert
        Assert.Equal(0, _viewModel.CurrentResultIndex);
        Assert.Equal("Match 1 of 3", _viewModel.StatusText);
    }

    [Fact]
    public void FindPrevious_WithResults_MovesToPreviousMatch()
    {
        // Arrange
        var documentText = "test test test";
        _viewModel.SearchText = "test";
        _viewModel.PerformSearch(documentText);

        var highlightedRanges = new System.Collections.Generic.List<TextRange>();
        _viewModel.HighlightRequested += (s, range) => highlightedRanges.Add(range);

        // Act
        _viewModel.FindPreviousCommand.Execute(null);

        // Assert
        Assert.Equal(2, _viewModel.CurrentResultIndex); // Wrapped to last
        Assert.Equal("Match 3 of 3", _viewModel.StatusText);
    }

    [Fact]
    public void FindNextCommand_WhenNoResults_CannotExecute()
    {
        // Arrange - no search performed

        // Act & Assert
        Assert.False(_viewModel.FindNextCommand.CanExecute(null));
    }

    [Fact]
    public void FindPreviousCommand_WhenNoResults_CannotExecute()
    {
        // Arrange - no search performed

        // Act & Assert
        Assert.False(_viewModel.FindPreviousCommand.CanExecute(null));
    }

    #endregion

    #region Replace Tests

    [Fact]
    public void ReplaceCommand_WithResults_RaisesReplaceEvent()
    {
        // Arrange
        var documentText = "test test test";
        _viewModel.SearchText = "test";
        _viewModel.ReplaceText = "exam";
        _viewModel.PerformSearch(documentText);

        var replaceEventArgs = default((string searchText, string replaceText, bool replaceAll));
        _viewModel.ReplaceRequested += (s, args) => replaceEventArgs = args;

        // Act
        _viewModel.ReplaceCommand.Execute(null);

        // Assert
        Assert.Equal("test", replaceEventArgs.searchText);
        Assert.Equal("exam", replaceEventArgs.replaceText);
        Assert.False(replaceEventArgs.replaceAll);
    }

    [Fact]
    public void ReplaceAllCommand_WithResults_RaisesReplaceAllEvent()
    {
        // Arrange
        var documentText = "test test test";
        _viewModel.SearchText = "test";
        _viewModel.ReplaceText = "exam";
        _viewModel.PerformSearch(documentText);

        var replaceEventArgs = default((string searchText, string replaceText, bool replaceAll));
        _viewModel.ReplaceRequested += (s, args) => replaceEventArgs = args;

        // Act
        _viewModel.ReplaceAllCommand.Execute(null);

        // Assert
        Assert.Equal("test", replaceEventArgs.searchText);
        Assert.Equal("exam", replaceEventArgs.replaceText);
        Assert.True(replaceEventArgs.replaceAll);
    }

    [Fact]
    public void ReplaceCommand_WhenNoResults_CannotExecute()
    {
        // Arrange - no search performed

        // Act & Assert
        Assert.False(_viewModel.ReplaceCommand.CanExecute(null));
    }

    [Fact]
    public void ReplaceAllCommand_WhenNoResults_CannotExecute()
    {
        // Arrange - no search performed

        // Act & Assert
        Assert.False(_viewModel.ReplaceAllCommand.CanExecute(null));
    }

    #endregion

    #region ClearResults Tests

    [Fact]
    public void ClearResults_ClearsSearchResultsAndState()
    {
        // Arrange
        var documentText = "test test test";
        _viewModel.SearchText = "test";
        _viewModel.PerformSearch(documentText);

        // Act
        _viewModel.ClearResults();

        // Assert
        Assert.Empty(_viewModel.SearchResults);
        Assert.Equal(-1, _viewModel.CurrentResultIndex);
        Assert.False(_viewModel.HasResults);
        Assert.Equal("Ready", _viewModel.StatusText);
    }

    #endregion

    #region Command CanExecute Updates

    [Fact]
    public void HasResults_WhenChanged_UpdatesCommandCanExecute()
    {
        // Arrange
        var documentText = "test test test";
        _viewModel.SearchText = "test";

        // Initially, commands should not be executable
        Assert.False(_viewModel.FindNextCommand.CanExecute(null));
        Assert.False(_viewModel.FindPreviousCommand.CanExecute(null));
        Assert.False(_viewModel.ReplaceCommand.CanExecute(null));
        Assert.False(_viewModel.ReplaceAllCommand.CanExecute(null));

        // Act - perform search to set HasResults = true
        _viewModel.PerformSearch(documentText);

        // Assert - commands should now be executable
        Assert.True(_viewModel.FindNextCommand.CanExecute(null));
        Assert.True(_viewModel.FindPreviousCommand.CanExecute(null));
        Assert.True(_viewModel.ReplaceCommand.CanExecute(null));
        Assert.True(_viewModel.ReplaceAllCommand.CanExecute(null));
    }

    #endregion
}
