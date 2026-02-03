using Avalonia.Controls;
using Avalonia.Platform.Storage;
using MiniWord.Core.Models;
using MiniWord.UI.ViewModels;
using Serilog;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MiniWord.UI.Views;

/// <summary>
/// Main window with MVVM pattern - uses data binding and property change notifications
/// Keyboard shortcuts remain in code-behind as they are view-specific behavior
/// </summary>
public partial class MainWindow : Window
{
    private readonly ILogger _logger;
    private readonly MainWindowViewModel _viewModel;

    public MainWindow()
    {
        _logger = Log.ForContext<MainWindow>();
        _logger.Information("MainWindow initializing...");

        InitializeComponent();

        _viewModel = new MainWindowViewModel();
        DataContext = _viewModel;

        // Wire up file dialog delegates
        _viewModel.ShowOpenFileDialogAsync = ShowOpenFileDialogAsync;
        _viewModel.ShowSaveFileDialogAsync = ShowSaveFileDialogAsync;
        _viewModel.ShowConfirmationDialogAsync = ShowConfirmationDialogAsync;
        _viewModel.CloseWindow = () => Close();

        // Subscribe to ViewModel property changes for margin updates
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        // Subscribe to validation errors (P3.3)
        _viewModel.ErrorsChanged += OnViewModelErrorsChanged;

        // Subscribe to recent files collection changes (P4.3)
        _viewModel.RecentFiles.CollectionChanged += OnRecentFilesCollectionChanged;

        // Wire up Find/Replace menu item (P5.2)
        var findReplaceMenuItem = this.FindControl<MenuItem>("FindReplaceMenuItem");
        if (findReplaceMenuItem != null)
        {
            findReplaceMenuItem.Click += (s, e) => ShowFindReplaceDialog();
        }

        // Wire up formatting buttons (P5.3)
        var boldButton = this.FindControl<Button>("BoldButton");
        if (boldButton != null)
        {
            boldButton.Click += (s, e) => ApplyBoldFormatting();
        }

        var italicButton = this.FindControl<Button>("ItalicButton");
        if (italicButton != null)
        {
            italicButton.Click += (s, e) => ApplyItalicFormatting();
        }

        var underlineButton = this.FindControl<Button>("UnderlineButton");
        if (underlineButton != null)
        {
            underlineButton.Click += (s, e) => ApplyUnderlineFormatting();
        }

        // Wire up keyboard event handlers (view-specific behavior)
        this.KeyDown += MainWindow_KeyDown;

        // Wire up window closing event to check for unsaved changes
        this.Closing += MainWindow_Closing;

        // Populate initial recent files menu (P4.3)
        PopulateRecentFilesMenu();

        // Initialize document in A4Canvas (P5.3)
        var canvas = this.FindControl<Controls.A4Canvas>("A4Canvas");
        if (canvas != null)
        {
            canvas.SetDocument(_viewModel.Document);
            _logger.Debug("Document initialized in A4Canvas");
        }

        _logger.Information("MainWindow initialized successfully with MVVM pattern, validation support, file operations, and recent files tracking");
    }

    /// <summary>
    /// Handles window closing event - prompts to save unsaved changes
    /// </summary>
    private async void MainWindow_Closing(object? sender, WindowClosingEventArgs e)
    {
        _logger.Information("Window closing event triggered");

        // If document is dirty, prompt to save
        if (_viewModel.IsDirty)
        {
            // Cancel the close event temporarily
            e.Cancel = true;

            try
            {
                var result = await ShowConfirmationDialogAsync(
                    "Unsaved Changes",
                    "Do you want to save changes before closing?");

                if (result)
                {
                    // User wants to save
                    await _viewModel.SaveAsync();
                    
                    // If still dirty (save was cancelled), don't close
                    if (_viewModel.IsDirty)
                    {
                        _logger.Information("Save cancelled, window will not close");
                        return;
                    }
                }

                // Close the window for real
                _logger.Information("Closing window after handling unsaved changes");
                this.Closing -= MainWindow_Closing; // Remove handler to avoid loop
                this.Close();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error handling window closing");
                this.Closing -= MainWindow_Closing; // Remove handler
                this.Close();
            }
        }
    }

    /// <summary>
    /// Handles validation errors from ViewModel - displays error messages in UI (P3.3)
    /// </summary>
    private void OnViewModelErrorsChanged(object? sender, DataErrorsChangedEventArgs e)
    {
        _logger.Debug("Validation errors changed for property: {PropertyName}", e.PropertyName);

        try
        {
            var leftErrorBlock = this.FindControl<TextBlock>("LeftMarginError");
            var rightErrorBlock = this.FindControl<TextBlock>("RightMarginError");

            if (e.PropertyName == nameof(_viewModel.LeftMarginMm) && leftErrorBlock != null)
            {
                var errors = _viewModel.GetErrors(nameof(_viewModel.LeftMarginMm))
                    .Cast<string>()
                    .ToList();

                leftErrorBlock.Text = errors.Count > 0 ? $"⚠ {errors.First()}" : string.Empty;
                leftErrorBlock.IsVisible = errors.Count > 0;

                _logger.Information("Left margin validation errors displayed: {Count} errors", errors.Count);
            }
            else if (e.PropertyName == nameof(_viewModel.RightMarginMm) && rightErrorBlock != null)
            {
                var errors = _viewModel.GetErrors(nameof(_viewModel.RightMarginMm))
                    .Cast<string>()
                    .ToList();

                rightErrorBlock.Text = errors.Count > 0 ? $"⚠ {errors.First()}" : string.Empty;
                rightErrorBlock.IsVisible = errors.Count > 0;

                _logger.Information("Right margin validation errors displayed: {Count} errors", errors.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to display validation errors for property: {PropertyName}", e.PropertyName);
        }
    }

    /// <summary>
    /// Handles property changes from ViewModel - updates UI controls when margins change
    /// </summary>
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // When Margins property changes, update the UI controls
        if (e.PropertyName == nameof(_viewModel.Margins))
        {
            _logger.Information("Margins property changed in ViewModel: {Margins}", _viewModel.Margins);

            try
            {
                var canvas = this.FindControl<Controls.A4Canvas>("A4Canvas");
                var ruler = this.FindControl<Controls.RulerControl>("RulerControl");

                canvas?.UpdateMargins(_viewModel.Margins);
                ruler?.UpdateMargins(_viewModel.Margins);

                _logger.Information("Margins applied to UI controls successfully");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to apply margins to UI controls");
            }
        }
    }

    /// <summary>
    /// Keyboard event handler - view-specific behavior remains in code-behind
    /// This is acceptable in MVVM as keyboard shortcuts are view-level concerns
    /// </summary>
    private void MainWindow_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        _logger.Debug("Key pressed: {Key}, Modifiers: {Modifiers}", e.Key, e.KeyModifiers);

        var canvas = this.FindControl<Controls.A4Canvas>("A4Canvas");
        if (canvas == null)
        {
            _logger.Warning("A4Canvas not found for keyboard handling");
            return;
        }

        // Ctrl+S for save (like WinForms)
        if (e.KeyModifiers == Avalonia.Input.KeyModifiers.Control && e.Key == Avalonia.Input.Key.S)
        {
            _logger.Information("Ctrl+S pressed - executing Save command");
            _ = _viewModel.SaveAsync();
            e.Handled = true;
            return;
        }

        // Ctrl+F for Find/Replace dialog (P5.2)
        if (e.KeyModifiers == Avalonia.Input.KeyModifiers.Control && e.Key == Avalonia.Input.Key.F)
        {
            _logger.Information("Ctrl+F pressed - opening Find/Replace dialog");
            ShowFindReplaceDialog();
            e.Handled = true;
            return;
        }

        // Ctrl+B for bold formatting (P5.3)
        if (e.KeyModifiers == Avalonia.Input.KeyModifiers.Control && e.Key == Avalonia.Input.Key.B)
        {
            _logger.Information("Ctrl+B pressed - toggling bold formatting");
            ApplyBoldFormatting();
            e.Handled = true;
            return;
        }

        // Ctrl+I for italic formatting (P5.3)
        if (e.KeyModifiers == Avalonia.Input.KeyModifiers.Control && e.Key == Avalonia.Input.Key.I)
        {
            _logger.Information("Ctrl+I pressed - toggling italic formatting");
            ApplyItalicFormatting();
            e.Handled = true;
            return;
        }

        // Ctrl+U for underline formatting (P5.3)
        if (e.KeyModifiers == Avalonia.Input.KeyModifiers.Control && e.Key == Avalonia.Input.Key.U)
        {
            _logger.Information("Ctrl+U pressed - toggling underline formatting");
            ApplyUnderlineFormatting();
            e.Handled = true;
            return;
        }

        // Ctrl+N for new document
        if (e.KeyModifiers == Avalonia.Input.KeyModifiers.Control && e.Key == Avalonia.Input.Key.N)
        {
            _logger.Information("Ctrl+N pressed - executing New command");
            _ = _viewModel.NewAsync();
            e.Handled = true;
            return;
        }

        // Ctrl+O for open document
        if (e.KeyModifiers == Avalonia.Input.KeyModifiers.Control && e.Key == Avalonia.Input.Key.O)
        {
            _logger.Information("Ctrl+O pressed - executing Open command");
            _ = _viewModel.OpenAsync();
            e.Handled = true;
            return;
        }

        // Ctrl+Home - Scroll to top of document
        if (e.KeyModifiers == Avalonia.Input.KeyModifiers.Control && e.Key == Avalonia.Input.Key.Home)
        {
            _logger.Information("Ctrl+Home pressed - Scrolling to top");
            canvas.ScrollToTop();
            e.Handled = true;
            return;
        }

        // Ctrl+End - Scroll to bottom of document
        if (e.KeyModifiers == Avalonia.Input.KeyModifiers.Control && e.Key == Avalonia.Input.Key.End)
        {
            _logger.Information("Ctrl+End pressed - Scrolling to bottom");
            canvas.ScrollToBottom();
            e.Handled = true;
            return;
        }

        // Page Up - Scroll up one page
        if (e.Key == Avalonia.Input.Key.PageUp)
        {
            _logger.Information("Page Up pressed - Scrolling up");
            canvas.ScrollPageUp();
            e.Handled = true;
            return;
        }

        // Page Down - Scroll down one page
        if (e.Key == Avalonia.Input.Key.PageDown)
        {
            _logger.Information("Page Down pressed - Scrolling down");
            canvas.ScrollPageDown();
            e.Handled = true;
            return;
        }

        // Other key handling can be added here
    }

    #region File Dialog Implementations

    /// <summary>
    /// Shows an open file dialog and returns the selected file path
    /// </summary>
    private async Task<string?> ShowOpenFileDialogAsync()
    {
        try
        {
            _logger.Information("Opening file dialog...");

            var storage = StorageProvider;
            var result = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open MiniWord Document",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("MiniWord Document")
                    {
                        Patterns = new[] { "*.miniword" }
                    },
                    new FilePickerFileType("All Files")
                    {
                        Patterns = new[] { "*.*" }
                    }
                }
            });

            if (result.Count > 0)
            {
                var filePath = result[0].Path.LocalPath;
                _logger.Information("File selected: {FilePath}", filePath);
                return filePath;
            }

            _logger.Information("No file selected");
            return null;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to show open file dialog");
            return null;
        }
    }

    /// <summary>
    /// Shows a save file dialog and returns the selected file path
    /// </summary>
    private async Task<string?> ShowSaveFileDialogAsync()
    {
        try
        {
            _logger.Information("Opening save file dialog...");

            var storage = StorageProvider;
            var result = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save MiniWord Document",
                DefaultExtension = "miniword",
                SuggestedFileName = "Untitled.miniword",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("MiniWord Document")
                    {
                        Patterns = new[] { "*.miniword" }
                    }
                }
            });

            if (result != null)
            {
                var filePath = result.Path.LocalPath;
                _logger.Information("File selected: {FilePath}", filePath);
                return filePath;
            }

            _logger.Information("No file selected");
            return null;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to show save file dialog");
            return null;
        }
    }

    /// <summary>
    /// Shows a confirmation dialog with Yes/No buttons
    /// </summary>
    private async Task<bool> ShowConfirmationDialogAsync(string title, string message)
    {
        try
        {
            _logger.Information("Showing confirmation dialog: {Title}", title);

            var dialog = new Window
            {
                Title = title,
                Width = 400,
                Height = 150,
                CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            bool result = false;
            var panel = new StackPanel
            {
                Margin = new Avalonia.Thickness(20),
                Spacing = 15
            };

            panel.Children.Add(new TextBlock
            {
                Text = message,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            });

            var buttonPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Spacing = 10
            };

            var yesButton = new Button
            {
                Content = "Yes",
                Width = 80,
                Padding = new Avalonia.Thickness(10, 5)
            };
            yesButton.Click += (s, e) =>
            {
                result = true;
                dialog.Close();
            };

            var noButton = new Button
            {
                Content = "No",
                Width = 80,
                Padding = new Avalonia.Thickness(10, 5)
            };
            noButton.Click += (s, e) =>
            {
                result = false;
                dialog.Close();
            };

            buttonPanel.Children.Add(yesButton);
            buttonPanel.Children.Add(noButton);
            panel.Children.Add(buttonPanel);

            dialog.Content = panel;
            await dialog.ShowDialog(this);

            _logger.Information("Confirmation dialog result: {Result}", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to show confirmation dialog");
            return false;
        }
    }

    #endregion

    #region Recent Files Menu (P4.3)

    /// <summary>
    /// Handles changes to the recent files collection - repopulates the menu
    /// </summary>
    private void OnRecentFilesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        _logger.Debug("Recent files collection changed");
        PopulateRecentFilesMenu();
    }

    /// <summary>
    /// Populates the "Open Recent" submenu with recent file entries
    /// </summary>
    private void PopulateRecentFilesMenu()
    {
        try
        {
            var openRecentMenuItem = this.FindControl<MenuItem>("OpenRecentMenuItem");
            if (openRecentMenuItem == null)
            {
                _logger.Warning("OpenRecentMenuItem not found in XAML");
                return;
            }

            // Clear existing items
            openRecentMenuItem.Items.Clear();

            // If no recent files, show "No recent files" disabled item
            if (_viewModel.RecentFiles.Count == 0)
            {
                var noFilesItem = new MenuItem
                {
                    Header = "No recent files",
                    IsEnabled = false
                };
                openRecentMenuItem.Items.Add(noFilesItem);
                _logger.Debug("No recent files to display");
                return;
            }

            // Add menu item for each recent file
            foreach (var filePath in _viewModel.RecentFiles)
            {
                var fileName = Path.GetFileName(filePath);
                var menuItem = new MenuItem
                {
                    Header = fileName,
                    Command = _viewModel.OpenRecentFileCommand,
                    CommandParameter = filePath
                };
                // Set tooltip using attached property
                ToolTip.SetTip(menuItem, filePath);
                openRecentMenuItem.Items.Add(menuItem);
            }

            _logger.Information("Populated recent files menu with {Count} items", _viewModel.RecentFiles.Count);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to populate recent files menu");
        }
    }

    #endregion

    #region Find/Replace Dialog (P5.2)

    /// <summary>
    /// Shows the Find/Replace dialog
    /// </summary>
    private void ShowFindReplaceDialog()
    {
        try
        {
            _logger.Information("Opening Find/Replace dialog");

            var canvas = this.FindControl<Controls.A4Canvas>("A4Canvas");
            if (canvas == null)
            {
                _logger.Warning("A4Canvas not found - cannot open Find/Replace dialog");
                return;
            }

            var findReplaceWindow = new FindReplaceWindow();
            
            // Wire up delegates
            findReplaceWindow.GetDocumentText = () => canvas.Text;
            findReplaceWindow.HighlightTextRange = (range) =>
            {
                _logger.Debug("Highlighting text range: {Range}", range);
                canvas.HighlightSearchResult(range);
            };
            findReplaceWindow.ReplaceText = (searchText, replaceText, replaceAll) =>
            {
                HandleReplaceText(canvas, searchText, replaceText, replaceAll, findReplaceWindow);
            };

            // Show dialog as modal
            _ = findReplaceWindow.ShowDialog(this);

            _logger.Information("Find/Replace dialog opened successfully");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to open Find/Replace dialog");
        }
    }

    /// <summary>
    /// Handles text replacement from Find/Replace dialog
    /// </summary>
    private void HandleReplaceText(Controls.A4Canvas canvas, string searchText, string replaceText, 
        bool replaceAll, FindReplaceWindow findReplaceWindow)
    {
        try
        {
            _logger.Information("Handling replace: searchText='{SearchText}', replaceText='{ReplaceText}', replaceAll={ReplaceAll}",
                searchText, replaceText, replaceAll);

            var documentText = canvas.Text;
            var searchEngine = new MiniWord.Core.Services.SearchEngine(_logger);
            var options = new MiniWord.Core.Services.SearchOptions
            {
                CaseSensitive = findReplaceWindow.GetViewModel().CaseSensitive,
                WholeWord = findReplaceWindow.GetViewModel().WholeWord,
                UseRegex = findReplaceWindow.GetViewModel().UseRegex
            };

            if (replaceAll)
            {
                // Replace all occurrences
                var (newText, replacedRanges) = searchEngine.ReplaceAll(documentText, searchText, replaceText, options);
                
                if (replacedRanges.Count > 0)
                {
                    canvas.Text = newText;
                    _viewModel.DocumentText = newText;
                    
                    _logger.Information("Replaced {Count} occurrences", replacedRanges.Count);
                    
                    // Refresh search results
                    findReplaceWindow.PerformSearch();
                }
                else
                {
                    _logger.Information("No occurrences to replace");
                }
            }
            else
            {
                // Replace first occurrence
                var (newText, replacedRange) = searchEngine.ReplaceFirst(documentText, searchText, replaceText, options);
                
                if (replacedRange != null)
                {
                    canvas.Text = newText;
                    _viewModel.DocumentText = newText;
                    
                    _logger.Information("Replaced single occurrence at position {Start}", replacedRange.Start);
                    
                    // Refresh search results and find next
                    findReplaceWindow.PerformSearch();
                }
                else
                {
                    _logger.Information("No occurrence to replace");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to replace text");
        }
    }

    #endregion

    #region Text Formatting (P5.3)

    /// <summary>
    /// Applies or removes bold formatting to the selected text
    /// </summary>
    private void ApplyBoldFormatting()
    {
        try
        {
            var canvas = this.FindControl<Controls.A4Canvas>("A4Canvas");
            if (canvas == null)
            {
                _logger.Warning("A4Canvas not found - cannot apply bold formatting");
                return;
            }

            canvas.ToggleBold();
            _logger.Information("Bold formatting toggled");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to apply bold formatting");
        }
    }

    /// <summary>
    /// Applies or removes italic formatting to the selected text
    /// </summary>
    private void ApplyItalicFormatting()
    {
        try
        {
            var canvas = this.FindControl<Controls.A4Canvas>("A4Canvas");
            if (canvas == null)
            {
                _logger.Warning("A4Canvas not found - cannot apply italic formatting");
                return;
            }

            canvas.ToggleItalic();
            _logger.Information("Italic formatting toggled");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to apply italic formatting");
        }
    }

    /// <summary>
    /// Applies or removes underline formatting to the selected text
    /// </summary>
    private void ApplyUnderlineFormatting()
    {
        try
        {
            var canvas = this.FindControl<Controls.A4Canvas>("A4Canvas");
            if (canvas == null)
            {
                _logger.Warning("A4Canvas not found - cannot apply underline formatting");
                return;
            }

            canvas.ToggleUnderline();
            _logger.Information("Underline formatting toggled");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to apply underline formatting");
        }
    }

    #endregion
}
