using Avalonia.Controls;
using MiniWord.Core.Models;
using MiniWord.UI.ViewModels;
using Serilog;
using System;
using System.ComponentModel;
using System.Linq;

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

        // Subscribe to ViewModel property changes for margin updates
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        // Subscribe to validation errors (P3.3)
        _viewModel.ErrorsChanged += OnViewModelErrorsChanged;

        // Wire up keyboard event handlers (view-specific behavior)
        this.KeyDown += MainWindow_KeyDown;

        _logger.Information("MainWindow initialized successfully with MVVM pattern and validation support");
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
            _logger.Information("Ctrl+S pressed - Save action (not implemented yet)");
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
}
