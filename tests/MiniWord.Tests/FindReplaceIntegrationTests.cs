using MiniWord.Core.Models;
using MiniWord.Core.Services;
using MiniWord.UI.ViewModels;
using MiniWord.UI.Views;
using Serilog;
using Xunit;

namespace MiniWord.Tests;

/// <summary>
/// Integration tests for Find/Replace dialog functionality - P5.2
/// Tests the complete workflow from UI to SearchEngine
/// </summary>
public class FindReplaceIntegrationTests : IDisposable
{
    private readonly ILogger _logger;
    private readonly SearchEngine _searchEngine;

    public FindReplaceIntegrationTests()
    {
        _logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

        _searchEngine = new SearchEngine(_logger);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    #region Complete Workflow Tests

    [Fact]
    public void FindReplaceWorkflow_CompleteScenario_WorksEndToEnd()
    {
        // Arrange
        var documentText = "The quick brown fox jumps over the lazy dog. The fox is clever.";
        var viewModel = new FindReplaceViewModel();

        // Track events
        var highlightedRanges = new System.Collections.Generic.List<TextRange>();
        var replaceRequests = new System.Collections.Generic.List<(string, string, bool)>();
        
        viewModel.HighlightRequested += (s, range) => highlightedRanges.Add(range);
        viewModel.ReplaceRequested += (s, args) => replaceRequests.Add(args);

        // Act 1: Search for "fox"
        viewModel.SearchText = "fox";
        viewModel.CaseSensitive = false;
        viewModel.PerformSearch(documentText);

        // Assert 1: Should find 2 occurrences
        Assert.Equal("Found 2 match(es)", viewModel.StatusText);
        Assert.Equal(2, viewModel.SearchResults.Count);
        Assert.Equal(0, viewModel.CurrentResultIndex);
        Assert.Single(highlightedRanges); // First match highlighted
        Assert.Equal(new TextRange(16, 19), highlightedRanges[0]);

        // Act 2: Find next
        highlightedRanges.Clear();
        viewModel.FindNextCommand.Execute(null);

        // Assert 2: Should move to second occurrence
        Assert.Equal(1, viewModel.CurrentResultIndex);
        Assert.Equal("Match 2 of 2", viewModel.StatusText);
        Assert.Single(highlightedRanges);
        Assert.Equal(new TextRange(49, 52), highlightedRanges[0]);

        // Act 3: Find previous
        highlightedRanges.Clear();
        viewModel.FindPreviousCommand.Execute(null);

        // Assert 3: Should move back to first occurrence
        Assert.Equal(0, viewModel.CurrentResultIndex);
        Assert.Equal("Match 1 of 2", viewModel.StatusText);
        Assert.Single(highlightedRanges);
        Assert.Equal(new TextRange(16, 19), highlightedRanges[0]);

        // Act 4: Replace current (first occurrence)
        viewModel.ReplaceText = "cat";
        viewModel.ReplaceCommand.Execute(null);

        // Assert 4: Should raise replace event
        Assert.Single(replaceRequests);
        Assert.Equal("fox", replaceRequests[0].Item1);
        Assert.Equal("cat", replaceRequests[0].Item2);
        Assert.False(replaceRequests[0].Item3); // Not replace all

        // Act 5: Replace all
        replaceRequests.Clear();
        viewModel.ReplaceAllCommand.Execute(null);

        // Assert 5: Should raise replace all event
        Assert.Single(replaceRequests);
        Assert.Equal("fox", replaceRequests[0].Item1);
        Assert.Equal("cat", replaceRequests[0].Item2);
        Assert.True(replaceRequests[0].Item3); // Replace all
    }

    [Fact]
    public void FindReplaceWorkflow_WithOptions_RespectsSearchOptions()
    {
        // Arrange
        var documentText = "Test test TEST testing";
        var viewModel = new FindReplaceViewModel();

        // Act 1: Case-insensitive search
        viewModel.SearchText = "test";
        viewModel.CaseSensitive = false;
        viewModel.PerformSearch(documentText);

        // Assert 1: Should find all variations
        Assert.Equal(4, viewModel.SearchResults.Count);

        // Act 2: Case-sensitive search
        viewModel.CaseSensitive = true;
        viewModel.PerformSearch(documentText);

        // Assert 2: Should find only exact matches
        Assert.Equal(2, viewModel.SearchResults.Count);

        // Act 3: Whole word search
        viewModel.CaseSensitive = false;
        viewModel.WholeWord = true;
        viewModel.PerformSearch(documentText);

        // Assert 3: Should find only complete words
        Assert.Equal(3, viewModel.SearchResults.Count);
    }

    [Fact]
    public void FindReplaceWorkflow_NavigationWrapAround_WorksCorrectly()
    {
        // Arrange
        var documentText = "a b c";
        var viewModel = new FindReplaceViewModel();
        viewModel.SearchText = "a|b|c";
        viewModel.UseRegex = true;
        viewModel.PerformSearch(documentText);

        // Should have 3 results at positions 0, 2, 4
        Assert.Equal(3, viewModel.SearchResults.Count);
        Assert.Equal(0, viewModel.CurrentResultIndex); // Start at first

        // Act 1: Navigate forward to end and wrap
        viewModel.FindNextCommand.Execute(null); // Index 1
        viewModel.FindNextCommand.Execute(null); // Index 2
        viewModel.FindNextCommand.Execute(null); // Should wrap to 0

        // Assert 1: Wrapped to first
        Assert.Equal(0, viewModel.CurrentResultIndex);

        // Act 2: Navigate backward from first and wrap
        viewModel.FindPreviousCommand.Execute(null); // Should wrap to 2

        // Assert 2: Wrapped to last
        Assert.Equal(2, viewModel.CurrentResultIndex);
    }

    [Fact]
    public void ReplaceWorkflow_SingleReplace_UpdatesDocument()
    {
        // Arrange
        var documentText = "Hello world, hello universe";
        var options = new SearchOptions { CaseSensitive = false };

        // Act: Replace first occurrence
        var (newText, replacedRange) = _searchEngine.ReplaceFirst(documentText, "hello", "hi", options);

        // Assert
        Assert.NotNull(replacedRange);
        Assert.Equal("hi world, hello universe", newText); // SearchEngine replaces with exact text, doesn't preserve case
        Assert.Equal(new TextRange(0, 2), replacedRange); // "hi" at position 0
    }

    [Fact]
    public void ReplaceWorkflow_ReplaceAll_UpdatesAllOccurrences()
    {
        // Arrange
        var documentText = "Hello world, hello universe, HELLO there";
        var options = new SearchOptions { CaseSensitive = false };

        // Act: Replace all occurrences
        var (newText, replacedRanges) = _searchEngine.ReplaceAll(documentText, "hello", "hi", options);

        // Assert
        Assert.Equal(3, replacedRanges.Count);
        Assert.Equal("hi world, hi universe, hi there", newText);
        
        // Verify each replacement position (positions shift after each replacement)
        Assert.Equal(new TextRange(0, 2), replacedRanges[0]);
        Assert.Equal(new TextRange(10, 12), replacedRanges[1]);
        Assert.Equal(new TextRange(23, 25), replacedRanges[2]); // Position shifts because "hello" (5 chars) -> "hi" (2 chars)
    }

    [Fact]
    public void FindReplaceWorkflow_EmptySearchText_HandlesGracefully()
    {
        // Arrange
        var viewModel = new FindReplaceViewModel();
        var documentText = "Some text";

        // Act
        viewModel.SearchText = "";
        viewModel.PerformSearch(documentText);

        // Assert
        Assert.Equal("Please enter search text", viewModel.StatusText);
        Assert.False(viewModel.HasResults);
        Assert.False(viewModel.FindNextCommand.CanExecute(null));
        Assert.False(viewModel.ReplaceCommand.CanExecute(null));
    }

    [Fact]
    public void FindReplaceWorkflow_NoMatches_ShowsAppropriateMessage()
    {
        // Arrange
        var viewModel = new FindReplaceViewModel();
        var documentText = "Some text";

        // Act
        viewModel.SearchText = "notfound";
        viewModel.PerformSearch(documentText);

        // Assert
        Assert.Equal("No matches found", viewModel.StatusText);
        Assert.False(viewModel.HasResults);
        Assert.Empty(viewModel.SearchResults);
    }

    [Fact]
    public void FindReplaceWorkflow_ClearResults_ResetsState()
    {
        // Arrange
        var viewModel = new FindReplaceViewModel();
        var documentText = "test test test";
        viewModel.SearchText = "test";
        viewModel.PerformSearch(documentText);

        Assert.True(viewModel.HasResults);
        Assert.Equal(3, viewModel.SearchResults.Count);

        // Act
        viewModel.ClearResults();

        // Assert
        Assert.False(viewModel.HasResults);
        Assert.Empty(viewModel.SearchResults);
        Assert.Equal(-1, viewModel.CurrentResultIndex);
        Assert.Equal("Ready", viewModel.StatusText);
        Assert.False(viewModel.FindNextCommand.CanExecute(null));
        Assert.False(viewModel.ReplaceCommand.CanExecute(null));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void FindReplaceWorkflow_OverlappingMatches_HandlesCorrectly()
    {
        // Arrange
        var documentText = "aaaa";
        var viewModel = new FindReplaceViewModel();

        // Act: Search for "aa" (should find overlapping matches)
        viewModel.SearchText = "aa";
        viewModel.PerformSearch(documentText);

        // Assert: SearchEngine should find overlapping matches
        Assert.True(viewModel.SearchResults.Count >= 2);
    }

    [Fact]
    public void FindReplaceWorkflow_SpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var documentText = "Price: $100.00, Total: $200.00";
        var viewModel = new FindReplaceViewModel();

        // Act: Search for literal "$" (not regex)
        viewModel.SearchText = "$";
        viewModel.UseRegex = false;
        viewModel.PerformSearch(documentText);

        // Assert
        Assert.Equal(2, viewModel.SearchResults.Count);
    }

    [Fact]
    public void FindReplaceWorkflow_MultilineText_HandlesCorrectly()
    {
        // Arrange
        var documentText = "Line 1 test\nLine 2 test\nLine 3 test";
        var viewModel = new FindReplaceViewModel();

        // Act
        viewModel.SearchText = "test";
        viewModel.PerformSearch(documentText);

        // Assert
        Assert.Equal(3, viewModel.SearchResults.Count);
        Assert.Equal(new TextRange(7, 11), viewModel.SearchResults[0]); // "test" in line 1
        Assert.Equal(new TextRange(19, 23), viewModel.SearchResults[1]); // "test" in line 2
        Assert.Equal(new TextRange(31, 35), viewModel.SearchResults[2]); // "test" in line 3
    }

    #endregion
}
