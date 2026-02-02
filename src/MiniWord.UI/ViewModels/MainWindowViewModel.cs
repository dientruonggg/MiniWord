using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
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
    private readonly DocumentSerializer _documentSerializer;
    private readonly RecentFilesManager _recentFilesManager;
    private readonly A4Document _document;
    
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

    [ObservableProperty]
    private string? _currentFilePath;

    [ObservableProperty]
    private bool _isDirty;

    /// <summary>
    /// Observable collection of recent files for UI binding (P4.3)
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<string> _recentFiles;

    /// <summary>
    /// Window title that reflects the current file and dirty state
    /// </summary>
    public string WindowTitle
    {
        get
        {
            var fileName = string.IsNullOrEmpty(CurrentFilePath) 
                ? "Untitled" 
                : System.IO.Path.GetFileName(CurrentFilePath);
            var dirtyMarker = IsDirty ? "*" : "";
            return $"{fileName}{dirtyMarker} - MiniWord";
        }
    }

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
        _documentSerializer = new DocumentSerializer(_logger);
        _recentFilesManager = new RecentFilesManager(_logger);
        _document = new A4Document(_logger);
        _margins = new DocumentMargins(); // Default 1 inch margins
        _documentText = string.Empty;
        _pageCount = 1;
        _wordCount = 0;
        _currentPage = 1;
        _currentFilePath = null;
        _isDirty = false;
        _recentFiles = new ObservableCollection<string>();

        // Load recent files from disk (P4.3)
        _recentFilesManager.Load();
        RefreshRecentFiles();

        // Subscribe to document property changes to track dirty state
        _document.PropertyChanged += OnDocumentPropertyChanged;

        _logger.Information("MainWindowViewModel initialized with MVVM pattern, validation support, file operations, and recent files tracking");
    }

    /// <summary>
    /// Handles document property changes to track dirty state
    /// </summary>
    private void OnDocumentPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(A4Document.IsDirty))
        {
            IsDirty = _document.IsDirty;
            _logger.Debug("Document dirty state changed to: {IsDirty}", IsDirty);
        }
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
        
        // Mark document as dirty when text changes
        if (!IsDirty)
        {
            IsDirty = true;
            _document.Content = value ?? string.Empty;
        }
    }

    partial void OnMarginsChanged(DocumentMargins value)
    {
        _logger.Information("Margins changed in ViewModel: {Margins}", value);
        // Also notify the mm properties that they may have changed
        OnPropertyChanged(nameof(LeftMarginMm));
        OnPropertyChanged(nameof(RightMarginMm));
    }

    partial void OnCurrentFilePathChanged(string? value)
    {
        _logger.Debug("Current file path changed: {FilePath}", value ?? "(null)");
        OnPropertyChanged(nameof(WindowTitle));
    }

    partial void OnIsDirtyChanged(bool value)
    {
        _logger.Debug("Is dirty changed: {IsDirty}", value);
        OnPropertyChanged(nameof(WindowTitle));
    }

    #region File Operations

    /// <summary>
    /// Delegate for showing file dialogs (injected by View)
    /// </summary>
    public Func<Task<string?>>? ShowOpenFileDialogAsync { get; set; }

    /// <summary>
    /// Delegate for showing save file dialogs (injected by View)
    /// </summary>
    public Func<Task<string?>>? ShowSaveFileDialogAsync { get; set; }

    /// <summary>
    /// Delegate for showing confirmation dialogs (injected by View)
    /// </summary>
    public Func<string, string, Task<bool>>? ShowConfirmationDialogAsync { get; set; }

    /// <summary>
    /// Delegate for closing the window (injected by View)
    /// </summary>
    public Action? CloseWindow { get; set; }

    /// <summary>
    /// Public method for Save (can be called from code-behind for keyboard shortcuts)
    /// </summary>
    public Task SaveAsync() => SaveInternalAsync();

    /// <summary>
    /// Public method for New (can be called from code-behind for keyboard shortcuts)
    /// </summary>
    public Task NewAsync() => NewInternalAsync();

    /// <summary>
    /// Public method for Open (can be called from code-behind for keyboard shortcuts)
    /// </summary>
    public Task OpenAsync() => OpenInternalAsync();

    /// <summary>
    /// Helper method to check for unsaved changes and prompt user to save
    /// </summary>
    /// <returns>True if should proceed, false if cancelled</returns>
    private async Task<bool> CheckUnsavedChangesAsync()
    {
        if (IsDirty && ShowConfirmationDialogAsync != null)
        {
            var result = await ShowConfirmationDialogAsync(
                "Unsaved Changes",
                "Do you want to save changes to the current document before opening another?");
            
            if (result)
            {
                // User wants to save - execute save command
                await SaveAsync();
                // If save was cancelled, don't proceed
                if (IsDirty) return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Helper method to load a document from file path
    /// </summary>
    /// <param name="filePath">Path to the file to load</param>
    private async Task LoadDocumentFromFileAsync(string filePath)
    {
        // Deserialize document
        var loadedDocument = await _documentSerializer.DeserializeAsync(filePath, _logger);
        
        // Update current document
        _document.Content = loadedDocument.Content;
        _document.UpdateMargins(loadedDocument.Margins);
        _document.Pages.Clear();
        foreach (var page in loadedDocument.Pages)
        {
            _document.Pages.Add(page);
        }
        _document.GoToPage(loadedDocument.CurrentPageIndex);
        _document.MarkAsSaved();

        // Update ViewModel
        DocumentText = loadedDocument.Content;
        Margins = loadedDocument.Margins;
        CurrentFilePath = filePath;
        IsDirty = false;
        UpdateWordCount();

        // Add to recent files (P4.3)
        _recentFilesManager.AddRecentFile(filePath);
        RefreshRecentFiles();
    }

    /// <summary>
    /// Command to create a new document
    /// </summary>
    [RelayCommand]
    private async Task NewInternalAsync()
    {
        _logger.Information("New command executed");

        try
        {
            // Check for unsaved changes
            if (IsDirty && ShowConfirmationDialogAsync != null)
            {
                var result = await ShowConfirmationDialogAsync(
                    "Unsaved Changes",
                    "Do you want to save changes to the current document before creating a new one?");
                
                if (result)
                {
                    // User wants to save - execute save command
                    await SaveAsync();
                    // If save was cancelled, don't proceed with new
                    if (IsDirty) return;
                }
            }

            // Create new document
            _document.Content = string.Empty;
            _document.Pages.Clear();
            _document.AddPage();
            _document.MarkAsSaved();
            
            // Update ViewModel
            DocumentText = string.Empty;
            CurrentFilePath = null;
            IsDirty = false;
            UpdateWordCount();

            _logger.Information("New document created successfully");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to create new document");
        }
    }

    /// <summary>
    /// Command to open an existing document
    /// </summary>
    [RelayCommand]
    private async Task OpenInternalAsync()
    {
        _logger.Information("Open command executed");

        try
        {
            // Check for unsaved changes
            if (!await CheckUnsavedChangesAsync())
                return;

            // Show open file dialog
            if (ShowOpenFileDialogAsync == null)
            {
                _logger.Warning("ShowOpenFileDialogAsync delegate not set");
                return;
            }

            var filePath = await ShowOpenFileDialogAsync();
            if (string.IsNullOrEmpty(filePath))
            {
                _logger.Information("Open file dialog cancelled");
                return;
            }

            // Load the document
            await LoadDocumentFromFileAsync(filePath);

            _logger.Information("Document opened successfully from {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to open document");
        }
    }

    /// <summary>
    /// Command to save the current document
    /// </summary>
    [RelayCommand]
    private async Task SaveInternalAsync()
    {
        _logger.Information("Save command executed");

        try
        {
            // If no file path, do Save As
            if (string.IsNullOrEmpty(CurrentFilePath))
            {
                await SaveAsInternalAsync();
                return;
            }

            // Update document content from UI
            _document.Content = DocumentText;
            _document.UpdateMargins(Margins);

            // Serialize document
            await _documentSerializer.SerializeAsync(_document, CurrentFilePath);
            
            // Mark as saved
            _document.MarkAsSaved();
            IsDirty = false;

            // Add to recent files (P4.3)
            _recentFilesManager.AddRecentFile(CurrentFilePath);
            RefreshRecentFiles();

            _logger.Information("Document saved successfully to {FilePath}", CurrentFilePath);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to save document");
        }
    }

    /// <summary>
    /// Command to save the current document with a new name
    /// </summary>
    [RelayCommand]
    private async Task SaveAsInternalAsync()
    {
        _logger.Information("SaveAs command executed");

        try
        {
            // Show save file dialog
            if (ShowSaveFileDialogAsync == null)
            {
                _logger.Warning("ShowSaveFileDialogAsync delegate not set");
                return;
            }

            var filePath = await ShowSaveFileDialogAsync();
            if (string.IsNullOrEmpty(filePath))
            {
                _logger.Information("Save file dialog cancelled");
                return;
            }

            // Update document content from UI
            _document.Content = DocumentText;
            _document.UpdateMargins(Margins);

            // Serialize document
            await _documentSerializer.SerializeAsync(_document, filePath);
            
            // Mark as saved and update file path
            _document.MarkAsSaved();
            CurrentFilePath = filePath;
            IsDirty = false;

            // Add to recent files (P4.3)
            _recentFilesManager.AddRecentFile(filePath);
            RefreshRecentFiles();

            _logger.Information("Document saved successfully to {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to save document as");
        }
    }

    /// <summary>
    /// Command to exit the application
    /// </summary>
    [RelayCommand]
    private async Task ExitInternalAsync()
    {
        _logger.Information("Exit command executed");

        try
        {
            // Check for unsaved changes
            if (IsDirty && ShowConfirmationDialogAsync != null)
            {
                var result = await ShowConfirmationDialogAsync(
                    "Unsaved Changes",
                    "Do you want to save changes before exiting?");
                
                if (result)
                {
                    // User wants to save - execute save command
                    await SaveAsync();
                    // If save was cancelled, don't exit
                    if (IsDirty) return;
                }
            }

            // Close the window
            CloseWindow?.Invoke();
            _logger.Information("Application exiting");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to exit application");
        }
    }

    /// <summary>
    /// Refreshes the observable collection of recent files from the manager (P4.3)
    /// </summary>
    private void RefreshRecentFiles()
    {
        try
        {
            RecentFiles.Clear();
            foreach (var file in _recentFilesManager.RecentFiles)
            {
                RecentFiles.Add(file);
            }
            
            _logger.Debug("Refreshed recent files list. Count: {Count}", RecentFiles.Count);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to refresh recent files");
        }
    }

    /// <summary>
    /// Command to open a file from the recent files list (P4.3)
    /// </summary>
    [RelayCommand]
    private async Task OpenRecentFileAsync(string filePath)
    {
        _logger.Information("OpenRecentFile command executed for: {FilePath}", filePath);

        try
        {
            // Check if file still exists
            if (!File.Exists(filePath))
            {
                _logger.Warning("Recent file no longer exists: {FilePath}", filePath);
                
                // Remove from recent files
                _recentFilesManager.RemoveRecentFile(filePath);
                RefreshRecentFiles();
                
                // TODO: Show error dialog to user
                return;
            }

            // Check for unsaved changes
            if (!await CheckUnsavedChangesAsync())
                return;

            // Load the document
            await LoadDocumentFromFileAsync(filePath);

            _logger.Information("Document opened from recent files: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to open recent file: {FilePath}", filePath);
        }
    }

    #endregion
}
