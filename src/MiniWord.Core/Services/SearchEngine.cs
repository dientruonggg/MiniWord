using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MiniWord.Core.Models;
using Serilog;

namespace MiniWord.Core.Services;

/// <summary>
/// Provides search and replace functionality with support for case-sensitive, 
/// whole word, and regex pattern matching.
/// </summary>
public class SearchEngine
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the SearchEngine
    /// </summary>
    /// <param name="logger">Logger instance for error and diagnostic logging</param>
    public SearchEngine(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Finds all occurrences of a search pattern in the given text
    /// </summary>
    /// <param name="text">The text to search in</param>
    /// <param name="searchPattern">The pattern to search for</param>
    /// <param name="options">Search options (case sensitivity, whole word, regex)</param>
    /// <returns>List of TextRange objects representing match positions</returns>
    public List<TextRange> FindAll(string text, string searchPattern, SearchOptions options)
    {
        var results = new List<TextRange>();

        if (string.IsNullOrEmpty(text))
        {
            _logger.Debug("FindAll: Empty text provided, returning empty results");
            return results;
        }

        if (string.IsNullOrEmpty(searchPattern))
        {
            _logger.Debug("FindAll: Empty search pattern provided, returning empty results");
            return results;
        }

        try
        {
            if (options.UseRegex)
            {
                results = FindWithRegex(text, searchPattern, options);
            }
            else if (options.WholeWord)
            {
                results = FindWholeWord(text, searchPattern, options);
            }
            else
            {
                results = FindSimple(text, searchPattern, options);
            }

            _logger.Debug("FindAll: Found {Count} matches for pattern '{Pattern}'", results.Count, searchPattern);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error during search operation for pattern '{Pattern}'", searchPattern);
            throw;
        }

        return results;
    }

    /// <summary>
    /// Replaces the first occurrence of a search pattern with replacement text
    /// </summary>
    /// <param name="text">The text to search in</param>
    /// <param name="searchPattern">The pattern to search for</param>
    /// <param name="replacement">The replacement text</param>
    /// <param name="options">Search options</param>
    /// <returns>The modified text and the position where replacement occurred (null if no match)</returns>
    public (string text, TextRange? replacedRange) ReplaceFirst(string text, string searchPattern, string replacement, SearchOptions options)
    {
        if (string.IsNullOrEmpty(text))
        {
            return (text, null);
        }

        if (string.IsNullOrEmpty(searchPattern))
        {
            return (text, null);
        }

        try
        {
            var matches = FindAll(text, searchPattern, options);
            if (matches.Count == 0)
            {
                _logger.Debug("ReplaceFirst: No matches found for pattern '{Pattern}'", searchPattern);
                return (text, null);
            }

            var firstMatch = matches[0];
            var before = text.Substring(0, firstMatch.Start);
            var after = text.Substring(firstMatch.End);
            var newText = before + replacement + after;

            var newRange = new TextRange(firstMatch.Start, firstMatch.Start + replacement.Length);
            _logger.Debug("ReplaceFirst: Replaced match at position {Start}", firstMatch.Start);

            return (newText, newRange);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error during replace operation for pattern '{Pattern}'", searchPattern);
            throw;
        }
    }

    /// <summary>
    /// Replaces all occurrences of a search pattern with replacement text
    /// </summary>
    /// <param name="text">The text to search in</param>
    /// <param name="searchPattern">The pattern to search for</param>
    /// <param name="replacement">The replacement text</param>
    /// <param name="options">Search options</param>
    /// <returns>The modified text and list of ranges where replacements occurred</returns>
    public (string text, List<TextRange> replacedRanges) ReplaceAll(string text, string searchPattern, string replacement, SearchOptions options)
    {
        var replacedRanges = new List<TextRange>();

        if (string.IsNullOrEmpty(text))
        {
            return (text, replacedRanges);
        }

        if (string.IsNullOrEmpty(searchPattern))
        {
            return (text, replacedRanges);
        }

        try
        {
            var matches = FindAll(text, searchPattern, options);
            if (matches.Count == 0)
            {
                _logger.Debug("ReplaceAll: No matches found for pattern '{Pattern}'", searchPattern);
                return (text, replacedRanges);
            }

            // Replace from end to start to maintain position indices
            var result = text;
            var offset = 0;

            for (int i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                var adjustedStart = match.Start + offset;
                var adjustedEnd = match.End + offset;

                var before = result.Substring(0, adjustedStart);
                var after = result.Substring(adjustedEnd);
                result = before + replacement + after;

                var newRange = new TextRange(adjustedStart, adjustedStart + replacement.Length);
                replacedRanges.Add(newRange);

                // Update offset for subsequent matches
                offset += replacement.Length - match.Length;
            }

            _logger.Debug("ReplaceAll: Replaced {Count} occurrences of pattern '{Pattern}'", matches.Count, searchPattern);
            return (result, replacedRanges);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error during replace all operation for pattern '{Pattern}'", searchPattern);
            throw;
        }
    }

    /// <summary>
    /// Simple string search without regex or whole word matching
    /// </summary>
    private List<TextRange> FindSimple(string text, string searchPattern, SearchOptions options)
    {
        var results = new List<TextRange>();
        var comparison = options.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        
        int index = 0;
        while (index < text.Length)
        {
            int foundIndex = text.IndexOf(searchPattern, index, comparison);
            if (foundIndex == -1)
                break;

            results.Add(new TextRange(foundIndex, foundIndex + searchPattern.Length));
            index = foundIndex + 1; // Move past this match to find overlapping matches
        }

        return results;
    }

    /// <summary>
    /// Search for whole word matches only
    /// </summary>
    private List<TextRange> FindWholeWord(string text, string searchPattern, SearchOptions options)
    {
        var results = new List<TextRange>();
        var comparison = options.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        int index = 0;
        while (index < text.Length)
        {
            int foundIndex = text.IndexOf(searchPattern, index, comparison);
            if (foundIndex == -1)
                break;

            // Check if this is a whole word match
            bool isStartBoundary = foundIndex == 0 || !char.IsLetterOrDigit(text[foundIndex - 1]);
            bool isEndBoundary = (foundIndex + searchPattern.Length >= text.Length) || 
                                 !char.IsLetterOrDigit(text[foundIndex + searchPattern.Length]);

            if (isStartBoundary && isEndBoundary)
            {
                results.Add(new TextRange(foundIndex, foundIndex + searchPattern.Length));
            }

            index = foundIndex + 1;
        }

        return results;
    }

    /// <summary>
    /// Search using regular expressions
    /// </summary>
    private List<TextRange> FindWithRegex(string text, string pattern, SearchOptions options)
    {
        var results = new List<TextRange>();

        try
        {
            var regexOptions = RegexOptions.None;
            if (!options.CaseSensitive)
            {
                regexOptions |= RegexOptions.IgnoreCase;
            }

            // Add timeout to prevent ReDoS attacks
            var regex = new Regex(pattern, regexOptions, TimeSpan.FromSeconds(5));
            var matches = regex.Matches(text);

            foreach (Match match in matches)
            {
                results.Add(new TextRange(match.Index, match.Index + match.Length));
            }
        }
        catch (RegexMatchTimeoutException ex)
        {
            _logger.Warning(ex, "Regex search timed out for pattern '{Pattern}'", pattern);
            throw new InvalidOperationException("Search operation timed out. The pattern may be too complex.", ex);
        }
        catch (ArgumentException ex)
        {
            _logger.Warning(ex, "Invalid regex pattern '{Pattern}'", pattern);
            throw new InvalidOperationException($"Invalid regular expression pattern: {ex.Message}", ex);
        }

        return results;
    }
}

/// <summary>
/// Options for configuring search behavior
/// </summary>
public class SearchOptions
{
    /// <summary>
    /// Gets or sets whether the search is case-sensitive
    /// </summary>
    public bool CaseSensitive { get; set; }

    /// <summary>
    /// Gets or sets whether to match whole words only
    /// </summary>
    public bool WholeWord { get; set; }

    /// <summary>
    /// Gets or sets whether to use regular expressions
    /// </summary>
    public bool UseRegex { get; set; }

    /// <summary>
    /// Creates default search options (case-insensitive, not whole word, not regex)
    /// </summary>
    public static SearchOptions Default => new SearchOptions
    {
        CaseSensitive = false,
        WholeWord = false,
        UseRegex = false
    };

    /// <summary>
    /// Creates case-sensitive search options
    /// </summary>
    public static SearchOptions CaseSensitiveSearch => new SearchOptions
    {
        CaseSensitive = true,
        WholeWord = false,
        UseRegex = false
    };

    /// <summary>
    /// Creates whole word search options
    /// </summary>
    public static SearchOptions WholeWordSearch => new SearchOptions
    {
        CaseSensitive = false,
        WholeWord = true,
        UseRegex = false
    };

    /// <summary>
    /// Creates regex search options
    /// </summary>
    public static SearchOptions RegexSearch => new SearchOptions
    {
        CaseSensitive = false,
        WholeWord = false,
        UseRegex = true
    };
}
