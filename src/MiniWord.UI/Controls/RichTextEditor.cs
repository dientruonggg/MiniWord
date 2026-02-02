using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Input;
using Serilog;
using System;

namespace MiniWord.UI.Controls;

/// <summary>
/// Custom rich text editor control for document editing
/// </summary>
public class RichTextEditor : TextBox
{
    private readonly ILogger _logger;

    public RichTextEditor()
    {
        _logger = Log.ForContext<RichTextEditor>();
        InitializeEditor();
    }

    private void InitializeEditor()
    {
        // Configure text box for document editing
        AcceptsReturn = true;
        TextWrapping = TextWrapping.Wrap;
        BorderThickness = new Thickness(0);
        Background = Brushes.Transparent;
        Padding = new Thickness(0);
        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
        FontFamily = new FontFamily("Times New Roman");
        FontSize = 12;

        _logger.Debug("RichTextEditor initialized");
    }

    /// <summary>
    /// Event raised when text cursor position changes
    /// </summary>
    public event EventHandler<CursorPositionChangedEventArgs>? CursorPositionChanged;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // Monitor caret index changes for visual feedback
        if (change.Property == CaretIndexProperty)
        {
            var caretIndex = (int)(change.NewValue ?? 0);
            CursorPositionChanged?.Invoke(this, new CursorPositionChangedEventArgs(caretIndex));
        }
    }
}

/// <summary>
/// Event arguments for cursor position changes
/// </summary>
public class CursorPositionChangedEventArgs : EventArgs
{
    public int CaretIndex { get; }

    public CursorPositionChangedEventArgs(int caretIndex)
    {
        CaretIndex = caretIndex;
    }
}
