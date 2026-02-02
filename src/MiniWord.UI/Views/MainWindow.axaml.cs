using Avalonia.Controls;
using Avalonia.Interactivity;
using MiniWord.Core.Models;
using MiniWord.UI.ViewModels;
using Serilog;
using System;

namespace MiniWord.UI.Views;

/// <summary>
/// Main window demonstrating Event/Delegate pattern (similar to WinForms)
/// This shows how WinForms KeyDown/KeyPress maps to Avalonia events
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

        // Wire up event handlers (Delegate/Event pattern like WinForms)
        SetupEventHandlers();

        _logger.Information("MainWindow initialized successfully");
    }

    /// <summary>
    /// Sets up event handlers - demonstrates C# Delegate/Event pattern
    /// Similar to WinForms: button.Click += Button_Click
    /// </summary>
    private void SetupEventHandlers()
    {
        _logger.Debug("Setting up event handlers");

        // Button click event (like WinForms)
        var applyButton = this.FindControl<Button>("ApplyMarginsButton");
        if (applyButton != null)
        {
            applyButton.Click += ApplyMarginsButton_Click;
            _logger.Debug("ApplyMarginsButton Click event handler attached");
        }

        // Keyboard events (mapping from WinForms to Avalonia)
        // WinForms: KeyDown event → Avalonia: KeyDown event (same name!)
        this.KeyDown += MainWindow_KeyDown;
        
        _logger.Information("All event handlers configured");
    }

    /// <summary>
    /// Event handler for Apply Margins button
    /// Demonstrates: EventHandler delegate pattern (like WinForms button.Click)
    /// </summary>
    private void ApplyMarginsButton_Click(object? sender, RoutedEventArgs e)
    {
        _logger.Information("Apply Margins button clicked");

        try
        {
            var leftMarginControl = this.FindControl<NumericUpDown>("LeftMarginInput");
            var rightMarginControl = this.FindControl<NumericUpDown>("RightMarginInput");

            if (leftMarginControl == null || rightMarginControl == null)
            {
                _logger.Error("Margin controls not found");
                return;
            }

            // Convert mm to pixels (96 DPI: 1 inch = 25.4mm = 96px)
            double mmToPixels = 96.0 / 25.4;
            double leftPx = Convert.ToDouble(leftMarginControl.Value ?? 25.4m) * mmToPixels;
            double rightPx = Convert.ToDouble(rightMarginControl.Value ?? 25.4m) * mmToPixels;

            var newMargins = new DocumentMargins(leftPx, rightPx, 96, 96);

            _logger.Information("Applying new margins: {Margins}", newMargins);

            // Update controls
            _currentMargins = newMargins;
            
            var canvas = this.FindControl<Controls.A4Canvas>("A4Canvas");
            var ruler = this.FindControl<Controls.RulerControl>("RulerControl");

            canvas?.UpdateMargins(newMargins);
            ruler?.UpdateMargins(newMargins);

            _logger.Information("Margins applied successfully. Text will reflow automatically.");
        }
        catch (System.Exception ex)
        {
            _logger.Error(ex, "Failed to apply margins");
        }
    }

    /// <summary>
    /// Keyboard event handler
    /// WinForms mapping: KeyDown event exists in both!
    /// WinForms: private void Form_KeyDown(object sender, KeyEventArgs e)
    /// Avalonia: private void Window_KeyDown(object sender, KeyEventArgs e)
    /// 
    /// The pattern is identical - just import Avalonia.Input for KeyEventArgs
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
        // Just like WinForms: check e.Key, e.KeyModifiers, etc.
    }
}
