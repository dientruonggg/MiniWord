using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiniWord.Core.Models;
using MiniWord.Core.Services;
using Serilog;

namespace MiniWord.UI.ViewModels;

/// <summary>
/// ViewModel for Find/Replace dialog - P5.2
/// Implements MVVM pattern with two-way binding and command support
/// </summary>
public partial class FindReplaceViewModel : ObservableObject
{
    private readonly ILogger _logger;
    private readonly SearchEngine _searchEngine;
    private List<TextRange> _searchResults;
    private int _currentResultIndex;

    [ObservableProperty]
    private string _searchText;

    [ObservableProperty]
    private string _replaceText;

    [ObservableProperty]
    private bool _caseSensitive;

    [ObservableProperty]
    private bool _wholeWord;

    [ObservableProperty]
    private bool _useRegex;

    [ObservableProperty]
    private string _statusText;

    [ObservableProperty]
    private bool _hasResults;

    /// <summary>
    /// Gets the current search results
    /// </summary>
    public IReadOnlyList<TextRange> SearchResults => _searchResults.AsReadOnly();

    /// <summary>
    /// Gets the current result index (0-based)
    /// </summary>
    public int CurrentResultIndex => _currentResultIndex;

    /// <summary>
    /// Event raised when user requests to highlight a search result
    /// </summary>
    public event EventHandler<TextRange>? HighlightRequested;

    /// <summary>
    /// Event raised when user requests to replace text
    /// </summary>
    public event EventHandler<(string searchText, string replaceText, bool replaceAll)>? ReplaceRequested;

    public FindReplaceViewModel()
    {
        _logger = Log.ForContext<FindReplaceViewModel>();
        _searchEngine = new SearchEngine(_logger);
        _searchResults = new List<TextRange>();
        _currentResultIndex = -1;
        _searchText = string.Empty;
        _replaceText = string.Empty;
        _statusText = "Ready";
        _hasResults = false;

        _logger.Information("FindReplaceViewModel initialized");
    }

    /// <summary>
    /// Performs a search on the given text
    /// </summary>
    public void PerformSearch(string documentText)
    {
        try
        {
            if (string.IsNullOrEmpty(SearchText))
            {
                StatusText = "Please enter search text";
                _searchResults.Clear();
                _currentResultIndex = -1;
                HasResults = false;
                _logger.Debug("Search aborted: empty search text");
                return;
            }

            if (string.IsNullOrEmpty(documentText))
            {
                StatusText = "Document is empty";
                _searchResults.Clear();
                _currentResultIndex = -1;
                HasResults = false;
                _logger.Debug("Search aborted: empty document");
                return;
            }

            var options = new SearchOptions
            {
                CaseSensitive = CaseSensitive,
                WholeWord = WholeWord,
                UseRegex = UseRegex
            };

            _searchResults = _searchEngine.FindAll(documentText, SearchText, options);
            
            if (_searchResults.Count > 0)
            {
                _currentResultIndex = 0;
                StatusText = $"Found {_searchResults.Count} match(es)";
                HasResults = true;
                
                // Highlight first result
                HighlightRequested?.Invoke(this, _searchResults[0]);
                
                _logger.Information("Search completed: found {Count} matches for '{Pattern}'", 
                    _searchResults.Count, SearchText);
            }
            else
            {
                _currentResultIndex = -1;
                StatusText = "No matches found";
                HasResults = false;
                _logger.Information("Search completed: no matches found for '{Pattern}'", SearchText);
            }
        }
        catch (InvalidOperationException ex)
        {
            // Handle regex errors and timeout errors
            StatusText = $"Search error: {ex.Message}";
            _searchResults.Clear();
            _currentResultIndex = -1;
            HasResults = false;
            _logger.Warning(ex, "Search operation failed");
        }
        catch (Exception ex)
        {
            StatusText = $"Unexpected error: {ex.Message}";
            _searchResults.Clear();
            _currentResultIndex = -1;
            HasResults = false;
            _logger.Error(ex, "Unexpected error during search");
        }
    }

    /// <summary>
    /// Command to find the next match
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanFindNext))]
    private void FindNext()
    {
        if (_searchResults.Count == 0) return;

        _currentResultIndex = (_currentResultIndex + 1) % _searchResults.Count;
        var result = _searchResults[_currentResultIndex];
        
        StatusText = $"Match {_currentResultIndex + 1} of {_searchResults.Count}";
        HighlightRequested?.Invoke(this, result);
        
        _logger.Debug("Find next: moved to result {Index} of {Total}", 
            _currentResultIndex + 1, _searchResults.Count);
    }

    private bool CanFindNext() => HasResults && _searchResults.Count > 0;

    /// <summary>
    /// Command to find the previous match
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanFindPrevious))]
    private void FindPrevious()
    {
        if (_searchResults.Count == 0) return;

        _currentResultIndex--;
        if (_currentResultIndex < 0)
        {
            _currentResultIndex = _searchResults.Count - 1;
        }
        
        var result = _searchResults[_currentResultIndex];
        
        StatusText = $"Match {_currentResultIndex + 1} of {_searchResults.Count}";
        HighlightRequested?.Invoke(this, result);
        
        _logger.Debug("Find previous: moved to result {Index} of {Total}", 
            _currentResultIndex + 1, _searchResults.Count);
    }

    private bool CanFindPrevious() => HasResults && _searchResults.Count > 0;

    /// <summary>
    /// Command to replace the current match
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanReplace))]
    private void Replace()
    {
        if (_searchResults.Count == 0 || _currentResultIndex < 0) return;

        _logger.Information("Replace requested for match {Index} of {Total}", 
            _currentResultIndex + 1, _searchResults.Count);
        
        ReplaceRequested?.Invoke(this, (SearchText, ReplaceText, false));
    }

    private bool CanReplace() => HasResults && _searchResults.Count > 0 && _currentResultIndex >= 0;

    /// <summary>
    /// Command to replace all matches
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanReplaceAll))]
    private void ReplaceAll()
    {
        if (_searchResults.Count == 0) return;

        _logger.Information("Replace all requested: {Count} matches", _searchResults.Count);
        
        ReplaceRequested?.Invoke(this, (SearchText, ReplaceText, true));
    }

    private bool CanReplaceAll() => HasResults && _searchResults.Count > 0;

    /// <summary>
    /// Clears the search results
    /// </summary>
    public void ClearResults()
    {
        _searchResults.Clear();
        _currentResultIndex = -1;
        HasResults = false;
        StatusText = "Ready";
        _logger.Debug("Search results cleared");
    }

    /// <summary>
    /// Notify commands to re-evaluate their CanExecute state
    /// </summary>
    partial void OnHasResultsChanged(bool value)
    {
        FindNextCommand.NotifyCanExecuteChanged();
        FindPreviousCommand.NotifyCanExecuteChanged();
        ReplaceCommand.NotifyCanExecuteChanged();
        ReplaceAllCommand.NotifyCanExecuteChanged();
    }
}
