using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
/// Implements INotifyDataErrorInfo for input validation (P3.3)
/// </summary>
public partial class MainWindowViewModel : ObservableObject, INotifyDataErrorInfo
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

    // Validation constants for margins (in millimeters)
    private const double MIN_MARGIN_MM = 0.0;
    private const double MAX_MARGIN_MM = 100.0;
    private const double A4_WIDTH_MM = 210.0;
    private const double A4_HEIGHT_MM = 297.0;

    // Error storage for INotifyDataErrorInfo
    private readonly Dictionary<string, List<string>> _errors = new();

    public MainWindowViewModel()
    {
        _logger = Log.ForContext<MainWindowViewModel>();
        _marginCalculator = new MarginCalculator(_logger);
        _margins = new DocumentMargins(); // Default 1 inch margins
        _documentText = string.Empty;
        _pageCount = 1;
        _wordCount = 0;
        _currentPage = 1;

        _logger.Information("MainWindowViewModel initialized with MVVM pattern and validation support");
    }

    #region INotifyDataErrorInfo Implementation

    /// <summary>
    /// Event raised when validation errors change
    /// </summary>
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    /// <summary>
    /// Gets whether the ViewModel has any validation errors
    /// </summary>
    public bool HasErrors => _errors.Count > 0;

    /// <summary>
    /// Gets validation errors for a specific property
    /// </summary>
    public IEnumerable GetErrors(string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
            return _errors.Values.SelectMany(e => e);

        return _errors.ContainsKey(propertyName) ? _errors[propertyName] : Enumerable.Empty<string>();
    }

    /// <summary>
    /// Adds a validation error for a property
    /// </summary>
    private void AddError(string propertyName, string error)
    {
        if (!_errors.ContainsKey(propertyName))
            _errors[propertyName] = new List<string>();

        if (!_errors[propertyName].Contains(error))
        {
            _errors[propertyName].Add(error);
            OnErrorsChanged(propertyName);
            _logger.Debug("Validation error added for {Property}: {Error}", propertyName, error);
        }
    }

    /// <summary>
    /// Clears validation errors for a property
    /// </summary>
    private void ClearErrors(string propertyName)
    {
        if (_errors.ContainsKey(propertyName))
        {
            _errors.Remove(propertyName);
            OnErrorsChanged(propertyName);
            _logger.Debug("Validation errors cleared for {Property}", propertyName);
        }
    }

    /// <summary>
    /// Raises the ErrorsChanged event
    /// </summary>
    private void OnErrorsChanged(string propertyName)
    {
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        OnPropertyChanged(nameof(HasErrors));
    }

    /// <summary>
    /// Validates a margin value
    /// </summary>
    private bool ValidateMarginValue(string propertyName, double value, string marginName)
    {
        ClearErrors(propertyName);

        // Check minimum value
        if (value < MIN_MARGIN_MM)
        {
            AddError(propertyName, $"{marginName} margin cannot be less than {MIN_MARGIN_MM}mm");
            return false;
        }

        // Check maximum value
        if (value > MAX_MARGIN_MM)
        {
            AddError(propertyName, $"{marginName} margin cannot exceed {MAX_MARGIN_MM}mm");
            return false;
        }

        // Check if margins would exceed paper dimensions
        if (propertyName == nameof(LeftMarginMm))
        {
            var totalHorizontal = value + RightMarginMm;
            if (totalHorizontal >= A4_WIDTH_MM)
            {
                AddError(propertyName, $"Left and right margins combined ({totalHorizontal:F1}mm) must be less than paper width ({A4_WIDTH_MM}mm)");
                return false;
            }
        }
        else if (propertyName == nameof(RightMarginMm))
        {
            var totalHorizontal = LeftMarginMm + value;
            if (totalHorizontal >= A4_WIDTH_MM)
            {
                AddError(propertyName, $"Left and right margins combined ({totalHorizontal:F1}mm) must be less than paper width ({A4_WIDTH_MM}mm)");
                return false;
            }
        }

        return true;
    }

    #endregion

    /// <summary>
    /// Left margin in millimeters (for UI binding)
    /// Two-way binding with NumericUpDown control
    /// Includes validation via INotifyDataErrorInfo (P3.3)
    /// </summary>
    public double LeftMarginMm
    {
        get => _marginCalculator.PixelsToMillimeters(Margins.Left);
        set
        {
            // Validate the new value
            if (!ValidateMarginValue(nameof(LeftMarginMm), value, "Left"))
            {
                _logger.Warning("Invalid left margin value: {Value}mm", value);
                OnPropertyChanged(); // Notify UI to show error
                return;
            }

            try
            {
                var pixels = _marginCalculator.MillimetersToPixels(value);
                var newMargins = new DocumentMargins(pixels, Margins.Right, Margins.Top, Margins.Bottom);
                Margins = newMargins;
                OnPropertyChanged();
                _logger.Debug("Left margin changed to {Value}mm ({Pixels}px)", value, pixels);
            }
            catch (Exception ex)
            {
                AddError(nameof(LeftMarginMm), $"Failed to set left margin: {ex.Message}");
                _logger.Error(ex, "Failed to set left margin to {Value}mm", value);
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Right margin in millimeters (for UI binding)
    /// Two-way binding with NumericUpDown control
    /// Includes validation via INotifyDataErrorInfo (P3.3)
    /// </summary>
    public double RightMarginMm
    {
        get => _marginCalculator.PixelsToMillimeters(Margins.Right);
        set
        {
            // Validate the new value
            if (!ValidateMarginValue(nameof(RightMarginMm), value, "Right"))
            {
                _logger.Warning("Invalid right margin value: {Value}mm", value);
                OnPropertyChanged(); // Notify UI to show error
                return;
            }

            try
            {
                var pixels = _marginCalculator.MillimetersToPixels(value);
                var newMargins = new DocumentMargins(Margins.Left, pixels, Margins.Top, Margins.Bottom);
                Margins = newMargins;
                OnPropertyChanged();
                _logger.Debug("Right margin changed to {Value}mm ({Pixels}px)", value, pixels);
            }
            catch (Exception ex)
            {
                AddError(nameof(RightMarginMm), $"Failed to set right margin: {ex.Message}");
                _logger.Error(ex, "Failed to set right margin to {Value}mm", value);
                OnPropertyChanged();
            }
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
