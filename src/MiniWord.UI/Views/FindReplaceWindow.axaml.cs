using Avalonia.Controls;
using Avalonia.Input;
using MiniWord.Core.Models;
using MiniWord.UI.ViewModels;
using Serilog;
using System;

namespace MiniWord.UI.Views;

/// <summary>
/// Find/Replace dialog window - P5.2
/// Modal dialog for searching and replacing text in the document
/// </summary>
public partial class FindReplaceWindow : Window
{
    private readonly ILogger _logger;
    private readonly FindReplaceViewModel _viewModel;

    /// <summary>
    /// Delegate for getting document text (injected by caller)
    /// </summary>
    public Func<string>? GetDocumentText { get; set; }

    /// <summary>
    /// Delegate for highlighting a text range (injected by caller)
    /// </summary>
    public Action<TextRange>? HighlightTextRange { get; set; }

    /// <summary>
    /// Delegate for replacing text (injected by caller)
    /// </summary>
    public Action<string, string, bool>? ReplaceText { get; set; }

    public FindReplaceWindow()
    {
        _logger = Log.ForContext<FindReplaceWindow>();
        _logger.Information("FindReplaceWindow initializing...");

        InitializeComponent();

        _viewModel = new FindReplaceViewModel();
        DataContext = _viewModel;

        // Subscribe to ViewModel events
        _viewModel.HighlightRequested += OnHighlightRequested;
        _viewModel.ReplaceRequested += OnReplaceRequested;

        // Subscribe to key events for Enter key handling
        this.KeyDown += FindReplaceWindow_KeyDown;

        _logger.Information("FindReplaceWindow initialized successfully");
    }

    /// <summary>
    /// Handles highlight requests from ViewModel
    /// </summary>
    private void OnHighlightRequested(object? sender, TextRange range)
    {
        _logger.Debug("Highlight requested for range: {Range}", range);
        HighlightTextRange?.Invoke(range);
    }

    /// <summary>
    /// Handles replace requests from ViewModel
    /// </summary>
    private void OnReplaceRequested(object? sender, (string searchText, string replaceText, bool replaceAll) args)
    {
        _logger.Information("Replace requested: searchText='{SearchText}', replaceText='{ReplaceText}', replaceAll={ReplaceAll}",
            args.searchText, args.replaceText, args.replaceAll);
        
        ReplaceText?.Invoke(args.searchText, args.replaceText, args.replaceAll);
    }

    /// <summary>
    /// Performs a search with the current search text
    /// </summary>
    public void PerformSearch()
    {
        try
        {
            if (GetDocumentText == null)
            {
                _logger.Warning("GetDocumentText delegate not set");
                return;
            }

            var documentText = GetDocumentText();
            _viewModel.PerformSearch(documentText);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to perform search");
        }
    }

    /// <summary>
    /// Handles keyboard shortcuts
    /// </summary>
    private void FindReplaceWindow_KeyDown(object? sender, KeyEventArgs e)
    {
        // Enter key in search textbox triggers search
        if (e.Key == Key.Enter)
        {
            var searchTextBox = this.FindControl<TextBox>("SearchTextBox");
            if (searchTextBox?.IsFocused == true)
            {
                _logger.Debug("Enter key pressed in search textbox - performing search");
                PerformSearch();
                e.Handled = true;
            }
        }
        // Escape key closes the dialog
        else if (e.Key == Key.Escape)
        {
            _logger.Debug("Escape key pressed - closing dialog");
            Close();
            e.Handled = true;
        }
        // F3 for Find Next
        else if (e.Key == Key.F3)
        {
            _logger.Debug("F3 key pressed - finding next");
            if (_viewModel.FindNextCommand.CanExecute(null))
            {
                _viewModel.FindNextCommand.Execute(null);
            }
            e.Handled = true;
        }
        // Shift+F3 for Find Previous
        else if (e.Key == Key.F3 && e.KeyModifiers == KeyModifiers.Shift)
        {
            _logger.Debug("Shift+F3 key pressed - finding previous");
            if (_viewModel.FindPreviousCommand.CanExecute(null))
            {
                _viewModel.FindPreviousCommand.Execute(null);
            }
            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles close button click
    /// </summary>
    private void CloseButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _logger.Information("Close button clicked");
        Close();
    }

    /// <summary>
    /// Gets the ViewModel instance
    /// </summary>
    public FindReplaceViewModel GetViewModel()
    {
        return _viewModel;
    }
}
