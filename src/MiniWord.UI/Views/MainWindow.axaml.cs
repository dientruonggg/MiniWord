using Avalonia.Controls;
using Avalonia.Interactivity;
using MiniWord.Core.Models;
using MiniWord.UI.ViewModels;
using Serilog;
using System;

namespace MiniWord.UI.Views;

/// <summary>
/// Main window with MVVM pattern - command binding replaces event handlers
/// Keyboard shortcuts remain in code-behind as they are view-specific behavior
/// </summary>
public partial class MainWindow : Window
{
    private readonly ILogger _logger;
    private readonly MainWindowViewModel _viewModel;
    private DocumentMargins _currentMargins;

    public MainWindow()
    {
        _logger = Log.ForContext<MainWindow>();
        _logger.Information("MainWindow initializing...");

        InitializeComponent();

        _viewModel = new MainWindowViewModel();
        DataContext = _viewModel;

        _currentMargins = new DocumentMargins(); // Default margins

        // Subscribe to ViewModel events
        _viewModel.MarginsApplied += OnMarginsApplied;

        // Wire up keyboard event handlers (view-specific behavior)
        SetupEventHandlers();

        _logger.Information("MainWindow initialized successfully with MVVM pattern");
    }

    /// <summary>
    /// Sets up keyboard event handlers - view-specific behavior
    /// </summary>
    private void SetupEventHandlers()
    {
        _logger.Debug("Setting up keyboard event handlers");

        // Keyboard events remain in code-behind as they are view-specific
        this.KeyDown += MainWindow_KeyDown;
        
        _logger.Information("Keyboard event handlers configured");
    }

    /// <summary>
    /// Event handler for when margins are applied via ViewModel command
    /// This updates the UI controls with the new margins
    /// </summary>
    private void OnMarginsApplied(object? sender, DocumentMargins margins)
    {
        _logger.Information("Margins applied event received: {Margins}", margins);

        try
        {
            _currentMargins = margins;
            
            var canvas = this.FindControl<Controls.A4Canvas>("A4Canvas");
            var ruler = this.FindControl<Controls.RulerControl>("RulerControl");

            canvas?.UpdateMargins(margins);
            ruler?.UpdateMargins(margins);

            _logger.Information("Margins applied to UI controls successfully");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to apply margins to UI controls");
        }
    }

    /// <summary>
    /// Keyboard event handler - view-specific behavior remains in code-behind
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
