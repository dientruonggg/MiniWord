using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiniWord.Core.Models;
using MiniWord.Core.Services;
using Serilog;

namespace MiniWord.UI.ViewModels;

/// <summary>
/// ViewModel for MainWindow - MVVM pattern with CommunityToolkit.Mvvm
/// Uses INotifyPropertyChanged for data binding with two-way margin controls
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    private readonly ILogger _logger;
    private readonly MarginCalculator _marginCalculator;
    
    [ObservableProperty]
    private DocumentMargins _margins;
    
    [ObservableProperty]
    private string _documentText;
    
    [ObservableProperty]
    private int _pageCount;
    
    [ObservableProperty]
    private int _wordCount;
    
    [ObservableProperty]
    private int _currentPage;

    public MainWindowViewModel()
    {
        _logger = Log.ForContext<MainWindowViewModel>();
        _marginCalculator = new MarginCalculator(_logger);
        _margins = new DocumentMargins(); // Default 1 inch margins
        _documentText = string.Empty;
        _pageCount = 1;
        _wordCount = 0;
        _currentPage = 1;

        _logger.Information("MainWindowViewModel initialized with MVVM pattern");
    }

    /// <summary>
    /// Left margin in millimeters (for UI binding)
    /// Two-way binding with NumericUpDown control
    /// </summary>
    public double LeftMarginMm
    {
        get => _marginCalculator.PixelsToMillimeters(Margins.Left);
        set
        {
            var pixels = _marginCalculator.MillimetersToPixels(value);
            var newMargins = new DocumentMargins(pixels, Margins.Right, Margins.Top, Margins.Bottom);
            Margins = newMargins;
            OnPropertyChanged();
            _logger.Debug("Left margin changed to {Value}mm ({Pixels}px)", value, pixels);
        }
    }

    /// <summary>
    /// Right margin in millimeters (for UI binding)
    /// Two-way binding with NumericUpDown control
    /// </summary>
    public double RightMarginMm
    {
        get => _marginCalculator.PixelsToMillimeters(Margins.Right);
        set
        {
            var pixels = _marginCalculator.MillimetersToPixels(value);
            var newMargins = new DocumentMargins(Margins.Left, pixels, Margins.Top, Margins.Bottom);
            Margins = newMargins;
            OnPropertyChanged();
            _logger.Debug("Right margin changed to {Value}mm ({Pixels}px)", value, pixels);
        }
    }

    /// <summary>
    /// Command to apply margins - triggers UI update via property change notification
    /// Replaces button click event handler from code-behind
    /// </summary>
    [RelayCommand]
    private void ApplyMargins()
    {
        _logger.Information("ApplyMargins command executed. Current margins: {Margins}", Margins);
        
        try
        {
            // Trigger property change notification to update UI
            // The View will receive this notification and update the controls
            OnPropertyChanged(nameof(Margins));
            
            _logger.Information("Margins property change notification sent successfully");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to send margins property change notification");
        }
    }

    /// <summary>
    /// Updates document statistics (called by UI layer)
    /// </summary>
    public void UpdateDocumentStats(int pageCount, int wordCount, int currentPage)
    {
        PageCount = pageCount;
        WordCount = wordCount;
        CurrentPage = currentPage;
        
        _logger.Debug("Document stats updated: {PageCount} pages, {WordCount} words, current page: {CurrentPage}", 
            pageCount, wordCount, currentPage);
    }

    /// <summary>
    /// Updates word count from document text
    /// </summary>
    public void UpdateWordCount()
    {
        if (string.IsNullOrWhiteSpace(DocumentText))
        {
            WordCount = 0;
        }
        else
        {
            // Simple word count: split by whitespace and count non-empty strings
            WordCount = DocumentText.Split(new[] { ' ', '\t', '\r', '\n' }, 
                StringSplitOptions.RemoveEmptyEntries).Length;
        }
        
        _logger.Debug("Word count updated: {WordCount} words", WordCount);
    }

    partial void OnDocumentTextChanged(string value)
    {
        _logger.Debug("Document text changed (length: {Length})", value?.Length ?? 0);
        UpdateWordCount();
    }

    partial void OnMarginsChanged(DocumentMargins value)
    {
        _logger.Information("Margins changed in ViewModel: {Margins}", value);
        // Also notify the mm properties that they may have changed
        OnPropertyChanged(nameof(LeftMarginMm));
        OnPropertyChanged(nameof(RightMarginMm));
    }
}
