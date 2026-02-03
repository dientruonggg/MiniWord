using MiniWord.Core.Models;
using MiniWord.Core.Services;
using Serilog;
using Xunit;

namespace MiniWord.Tests;

public class SearchEngineTests : IDisposable
{
    private readonly ILogger _logger;
    private readonly SearchEngine _searchEngine;

    public SearchEngineTests()
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

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SearchEngine(null!));
    }

    #endregion

    #region FindAll - Simple Search Tests

    [Fact]
    public void FindAll_WithEmptyText_ReturnsEmptyList()
    {
        // Arrange
        var options = SearchOptions.Default;

        // Act
        var results = _searchEngine.FindAll("", "test", options);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void FindAll_WithEmptyPattern_ReturnsEmptyList()
    {
        // Arrange
        var options = SearchOptions.Default;

        // Act
        var results = _searchEngine.FindAll("test text", "", options);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void FindAll_WithNoMatches_ReturnsEmptyList()
    {
        // Arrange
        var text = "hello world";
        var options = SearchOptions.Default;

        // Act
        var results = _searchEngine.FindAll(text, "xyz", options);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void FindAll_CaseInsensitive_FindsAllMatches()
    {
        // Arrange
        var text = "Hello world, HELLO universe, hello there";
        var options = new SearchOptions { CaseSensitive = false };

        // Act
        var results = _searchEngine.FindAll(text, "hello", options);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal(new TextRange(0, 5), results[0]);
        Assert.Equal(new TextRange(13, 18), results[1]);
        Assert.Equal(new TextRange(29, 34), results[2]);
    }

    [Fact]
    public void FindAll_CaseSensitive_FindsExactMatches()
    {
        // Arrange
        var text = "Hello world, HELLO universe, hello there";
        var options = SearchOptions.CaseSensitiveSearch;

        // Act
        var results = _searchEngine.FindAll(text, "hello", options);

        // Assert
        Assert.Single(results);
        Assert.Equal(new TextRange(29, 34), results[0]);
    }

    [Fact]
    public void FindAll_OverlappingMatches_FindsAll()
    {
        // Arrange
        var text = "aaaa";
        var options = SearchOptions.Default;

        // Act
        var results = _searchEngine.FindAll(text, "aa", options);

        // Assert
        Assert.Equal(3, results.Count); // aa at 0, aa at 1, aa at 2
        Assert.Equal(new TextRange(0, 2), results[0]);
        Assert.Equal(new TextRange(1, 3), results[1]);
        Assert.Equal(new TextRange(2, 4), results[2]);
    }

    [Fact]
    public void FindAll_SingleCharacter_FindsAllOccurrences()
    {
        // Arrange
        var text = "a b a c a";
        var options = SearchOptions.Default;

        // Act
        var results = _searchEngine.FindAll(text, "a", options);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal(new TextRange(0, 1), results[0]);
        Assert.Equal(new TextRange(4, 5), results[1]);
        Assert.Equal(new TextRange(8, 9), results[2]);
    }

    #endregion

    #region FindAll - Whole Word Tests

    [Fact]
    public void FindAll_WholeWord_MatchesOnlyCompleteWords()
    {
        // Arrange
        var text = "hello helloworld world hello";
        var options = SearchOptions.WholeWordSearch;

        // Act
        var results = _searchEngine.FindAll(text, "hello", options);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Equal(new TextRange(0, 5), results[0]);
        Assert.Equal(new TextRange(23, 28), results[1]);
    }

    [Fact]
    public void FindAll_WholeWord_AtStartOfText()
    {
        // Arrange
        var text = "word is here";
        var options = SearchOptions.WholeWordSearch;

        // Act
        var results = _searchEngine.FindAll(text, "word", options);

        // Assert
        Assert.Single(results);
        Assert.Equal(new TextRange(0, 4), results[0]);
    }

    [Fact]
    public void FindAll_WholeWord_AtEndOfText()
    {
        // Arrange
        var text = "here is word";
        var options = SearchOptions.WholeWordSearch;

        // Act
        var results = _searchEngine.FindAll(text, "word", options);

        // Assert
        Assert.Single(results);
        Assert.Equal(new TextRange(8, 12), results[0]);
    }

    [Fact]
    public void FindAll_WholeWord_WithPunctuation()
    {
        // Arrange
        var text = "hello, world! hello.";
        var options = SearchOptions.WholeWordSearch;

        // Act
        var results = _searchEngine.FindAll(text, "hello", options);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Equal(new TextRange(0, 5), results[0]);
        Assert.Equal(new TextRange(14, 19), results[1]);
    }

    [Fact]
    public void FindAll_WholeWord_DoesNotMatchPartialWords()
    {
        // Arrange
        var text = "helloing worldhello helloworld";
        var options = SearchOptions.WholeWordSearch;

        // Act
        var results = _searchEngine.FindAll(text, "hello", options);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void FindAll_WholeWord_CaseInsensitive()
    {
        // Arrange
        var text = "Hello world, HELLO universe";
        var options = new SearchOptions { WholeWord = true, CaseSensitive = false };

        // Act
        var results = _searchEngine.FindAll(text, "hello", options);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Equal(new TextRange(0, 5), results[0]);
        Assert.Equal(new TextRange(13, 18), results[1]);
    }

    #endregion

    #region FindAll - Regex Tests

    [Fact]
    public void FindAll_Regex_SimplePattern()
    {
        // Arrange
        var text = "test123 test456 test789";
        var options = SearchOptions.RegexSearch;

        // Act
        var results = _searchEngine.FindAll(text, @"test\d+", options);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal(new TextRange(0, 7), results[0]);
        Assert.Equal(new TextRange(8, 15), results[1]);
        Assert.Equal(new TextRange(16, 23), results[2]);
    }

    [Fact]
    public void FindAll_Regex_CaseInsensitive()
    {
        // Arrange
        var text = "Hello WORLD hello world";
        var options = new SearchOptions { UseRegex = true, CaseSensitive = false };

        // Act
        var results = _searchEngine.FindAll(text, @"hello", options);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Equal(new TextRange(0, 5), results[0]);
        Assert.Equal(new TextRange(12, 17), results[1]);
    }

    [Fact]
    public void FindAll_Regex_CaseSensitive()
    {
        // Arrange
        var text = "Hello WORLD hello world";
        var options = new SearchOptions { UseRegex = true, CaseSensitive = true };

        // Act
        var results = _searchEngine.FindAll(text, @"hello", options);

        // Assert
        Assert.Single(results);
        Assert.Equal(new TextRange(12, 17), results[0]);
    }

    [Fact]
    public void FindAll_Regex_ComplexPattern()
    {
        // Arrange
        var text = "email: test@example.com and user@domain.org";
        var options = SearchOptions.RegexSearch;

        // Act
        var results = _searchEngine.FindAll(text, @"\w+@\w+\.\w+", options);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Equal(new TextRange(7, 23), results[0]);
        Assert.Equal(new TextRange(28, 43), results[1]);
    }

    [Fact]
    public void FindAll_Regex_InvalidPattern_ThrowsException()
    {
        // Arrange
        var text = "test text";
        var options = SearchOptions.RegexSearch;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            _searchEngine.FindAll(text, "[invalid", options));
    }

    [Fact]
    public void FindAll_Regex_WordBoundary()
    {
        // Arrange
        var text = "hello helloworld world";
        var options = SearchOptions.RegexSearch;

        // Act
        var results = _searchEngine.FindAll(text, @"\bhello\b", options);

        // Assert
        Assert.Single(results);
        Assert.Equal(new TextRange(0, 5), results[0]);
    }

    #endregion

    #region ReplaceFirst Tests

    [Fact]
    public void ReplaceFirst_WithMatch_ReplacesFirstOccurrence()
    {
        // Arrange
        var text = "hello world hello universe";
        var options = SearchOptions.Default;

        // Act
        var (result, range) = _searchEngine.ReplaceFirst(text, "hello", "hi", options);

        // Assert
        Assert.Equal("hi world hello universe", result);
        Assert.NotNull(range);
        Assert.Equal(new TextRange(0, 2), range);
    }

    [Fact]
    public void ReplaceFirst_WithNoMatch_ReturnsOriginalText()
    {
        // Arrange
        var text = "hello world";
        var options = SearchOptions.Default;

        // Act
        var (result, range) = _searchEngine.ReplaceFirst(text, "xyz", "abc", options);

        // Assert
        Assert.Equal(text, result);
        Assert.Null(range);
    }

    [Fact]
    public void ReplaceFirst_WithEmptyText_ReturnsEmpty()
    {
        // Arrange
        var options = SearchOptions.Default;

        // Act
        var (result, range) = _searchEngine.ReplaceFirst("", "test", "replacement", options);

        // Assert
        Assert.Equal("", result);
        Assert.Null(range);
    }

    [Fact]
    public void ReplaceFirst_CaseSensitive_ReplacesExactMatch()
    {
        // Arrange
        var text = "Hello world hello";
        var options = SearchOptions.CaseSensitiveSearch;

        // Act
        var (result, range) = _searchEngine.ReplaceFirst(text, "hello", "hi", options);

        // Assert
        Assert.Equal("Hello world hi", result);
        Assert.NotNull(range);
        Assert.Equal(new TextRange(12, 14), range);
    }

    [Fact]
    public void ReplaceFirst_LongerReplacement_UpdatesCorrectly()
    {
        // Arrange
        var text = "hi world";
        var options = SearchOptions.Default;

        // Act
        var (result, range) = _searchEngine.ReplaceFirst(text, "hi", "hello", options);

        // Assert
        Assert.Equal("hello world", result);
        Assert.NotNull(range);
        Assert.Equal(new TextRange(0, 5), range);
    }

    #endregion

    #region ReplaceAll Tests

    [Fact]
    public void ReplaceAll_WithMatches_ReplacesAllOccurrences()
    {
        // Arrange
        var text = "hello world hello universe hello";
        var options = SearchOptions.Default;

        // Act
        var (result, ranges) = _searchEngine.ReplaceAll(text, "hello", "hi", options);

        // Assert
        Assert.Equal("hi world hi universe hi", result);
        Assert.Equal(3, ranges.Count);
        Assert.Equal(new TextRange(0, 2), ranges[0]);
        Assert.Equal(new TextRange(9, 11), ranges[1]);
        Assert.Equal(new TextRange(21, 23), ranges[2]);
    }

    [Fact]
    public void ReplaceAll_WithNoMatches_ReturnsOriginalText()
    {
        // Arrange
        var text = "hello world";
        var options = SearchOptions.Default;

        // Act
        var (result, ranges) = _searchEngine.ReplaceAll(text, "xyz", "abc", options);

        // Assert
        Assert.Equal(text, result);
        Assert.Empty(ranges);
    }

    [Fact]
    public void ReplaceAll_CaseInsensitive_ReplacesAllVariants()
    {
        // Arrange
        var text = "Hello HELLO hello";
        var options = new SearchOptions { CaseSensitive = false };

        // Act
        var (result, ranges) = _searchEngine.ReplaceAll(text, "hello", "hi", options);

        // Assert
        Assert.Equal("hi hi hi", result);
        Assert.Equal(3, ranges.Count);
    }

    [Fact]
    public void ReplaceAll_WholeWord_ReplacesOnlyWholeWords()
    {
        // Arrange
        var text = "hello helloworld world";
        var options = SearchOptions.WholeWordSearch;

        // Act
        var (result, ranges) = _searchEngine.ReplaceAll(text, "hello", "hi", options);

        // Assert
        Assert.Equal("hi helloworld world", result);
        Assert.Single(ranges);
        Assert.Equal(new TextRange(0, 2), ranges[0]);
    }

    [Fact]
    public void ReplaceAll_Regex_ReplacesPatternMatches()
    {
        // Arrange
        var text = "test123 test456 test789";
        var options = SearchOptions.RegexSearch;

        // Act
        var (result, ranges) = _searchEngine.ReplaceAll(text, @"test\d+", "item", options);

        // Assert
        Assert.Equal("item item item", result);
        Assert.Equal(3, ranges.Count);
    }

    [Fact]
    public void ReplaceAll_WithEmptyReplacement_RemovesMatches()
    {
        // Arrange
        var text = "hello world hello";
        var options = SearchOptions.Default;

        // Act
        var (result, ranges) = _searchEngine.ReplaceAll(text, "hello", "", options);

        // Assert
        Assert.Equal(" world ", result);
        Assert.Equal(2, ranges.Count);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void FindAll_UnicodeText_HandlesCorrectly()
    {
        // Arrange
        var text = "Привет мир Привет вселенная";
        var options = SearchOptions.Default;

        // Act
        var results = _searchEngine.FindAll(text, "Привет", options);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Equal(new TextRange(0, 6), results[0]);
        Assert.Equal(new TextRange(11, 17), results[1]);
    }

    [Fact]
    public void FindAll_SpecialCharacters_FindsMatches()
    {
        // Arrange
        var text = "a+b=c a+b=c";
        var options = SearchOptions.Default;

        // Act
        var results = _searchEngine.FindAll(text, "a+b", options);

        // Assert
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void ReplaceAll_ShorterReplacement_AdjustsRangesCorrectly()
    {
        // Arrange
        var text = "hello hello hello";
        var options = SearchOptions.Default;

        // Act
        var (result, ranges) = _searchEngine.ReplaceAll(text, "hello", "hi", options);

        // Assert
        Assert.Equal("hi hi hi", result);
        Assert.Equal(3, ranges.Count);
        Assert.Equal(new TextRange(0, 2), ranges[0]);
        Assert.Equal(new TextRange(3, 5), ranges[1]);
        Assert.Equal(new TextRange(6, 8), ranges[2]);
    }

    [Fact]
    public void ReplaceAll_LongerReplacement_AdjustsRangesCorrectly()
    {
        // Arrange
        var text = "hi hi hi";
        var options = SearchOptions.Default;

        // Act
        var (result, ranges) = _searchEngine.ReplaceAll(text, "hi", "hello", options);

        // Assert
        Assert.Equal("hello hello hello", result);
        Assert.Equal(3, ranges.Count);
        Assert.Equal(new TextRange(0, 5), ranges[0]);
        Assert.Equal(new TextRange(6, 11), ranges[1]);
        Assert.Equal(new TextRange(12, 17), ranges[2]);
    }

    #endregion

    #region SearchOptions Tests

    [Fact]
    public void SearchOptions_Default_HasCorrectSettings()
    {
        // Act
        var options = SearchOptions.Default;

        // Assert
        Assert.False(options.CaseSensitive);
        Assert.False(options.WholeWord);
        Assert.False(options.UseRegex);
    }

    [Fact]
    public void SearchOptions_CaseSensitiveSearch_HasCorrectSettings()
    {
        // Act
        var options = SearchOptions.CaseSensitiveSearch;

        // Assert
        Assert.True(options.CaseSensitive);
        Assert.False(options.WholeWord);
        Assert.False(options.UseRegex);
    }

    [Fact]
    public void SearchOptions_WholeWordSearch_HasCorrectSettings()
    {
        // Act
        var options = SearchOptions.WholeWordSearch;

        // Assert
        Assert.False(options.CaseSensitive);
        Assert.True(options.WholeWord);
        Assert.False(options.UseRegex);
    }

    [Fact]
    public void SearchOptions_RegexSearch_HasCorrectSettings()
    {
        // Act
        var options = SearchOptions.RegexSearch;

        // Assert
        Assert.False(options.CaseSensitive);
        Assert.False(options.WholeWord);
        Assert.True(options.UseRegex);
    }

    #endregion
}
