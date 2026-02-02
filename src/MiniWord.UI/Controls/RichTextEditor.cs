using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Input;
using Serilog;
using System;
using MiniWord.Core.Models;

namespace MiniWord.UI.Controls;

/// <summary>
/// Custom rich text editor control for document editing with cursor and selection management
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

    #region Cursor and Selection Management

    /// <summary>
    /// Event raised when text cursor position changes
    /// </summary>
    public event EventHandler<CursorPositionChangedEventArgs>? CursorPositionChanged;

    /// <summary>
    /// Event raised when text selection changes
    /// </summary>
    public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;

    /// <summary>
    /// Gets the current selection start position (maps to WinForms SelectionStart)
    /// </summary>
    public new int SelectionStart
    {
        get => Math.Min(SelectionStart_Internal, SelectionEnd_Internal);
    }

    /// <summary>
    /// Gets the current selection length (maps to WinForms SelectionLength)
    /// </summary>
    public int SelectionLength
    {
        get => Math.Abs(SelectionEnd_Internal - SelectionStart_Internal);
    }

    /// <summary>
    /// Gets the internal selection start (may be > end if selecting backwards)
    /// </summary>
    private int SelectionStart_Internal => SelectionStart_Property;

    /// <summary>
    /// Gets the internal selection end
    /// </summary>
    private int SelectionEnd_Internal => SelectionEnd_Property;

    // Internal properties to track Avalonia's SelectionStart and SelectionEnd
    private int SelectionStart_Property => GetValue(SelectionStartProperty);
    private int SelectionEnd_Property => GetValue(SelectionEndProperty);

    /// <summary>
    /// Gets the currently selected text
    /// </summary>
    public string GetSelectedText()
    {
        var text = Text ?? string.Empty;
        var start = SelectionStart;
        var length = SelectionLength;

        if (length == 0 || start >= text.Length)
        {
            return string.Empty;
        }

        var actualLength = Math.Min(length, text.Length - start);
        return text.Substring(start, actualLength);
    }

    /// <summary>
    /// Gets the current selection as a TextSelection object
    /// </summary>
    public TextSelection GetSelection()
    {
        return new TextSelection(SelectionStart, SelectionStart + SelectionLength);
    }

    /// <summary>
    /// Sets the text selection (maps to WinForms Select method)
    /// </summary>
    /// <param name="start">Start position</param>
    /// <param name="length">Selection length</param>
    public void SetSelection(int start, int length)
    {
        if (start < 0)
        {
            _logger.Warning("Attempted to set negative selection start: {Start}", start);
            start = 0;
        }

        if (length < 0)
        {
            _logger.Warning("Attempted to set negative selection length: {Length}", length);
            length = 0;
        }

        var text = Text ?? string.Empty;
        start = Math.Min(start, text.Length);
        length = Math.Min(length, text.Length - start);

        // Use Avalonia's selection properties
        SetValue(SelectionStartProperty, start);
        SetValue(SelectionEndProperty, start + length);

        _logger.Debug("Selection set: Start={Start}, Length={Length}", start, length);
    }

    /// <summary>
    /// Moves the caret to the specified position
    /// </summary>
    /// <param name="position">Target position</param>
    public void MoveCaretTo(int position)
    {
        var text = Text ?? string.Empty;
        position = Math.Max(0, Math.Min(position, text.Length));
        
        CaretIndex = position;
        _logger.Debug("Caret moved to position: {Position}", position);
    }

    /// <summary>
    /// Selects all text in the editor
    /// </summary>
    public new void SelectAll()
    {
        var text = Text ?? string.Empty;
        SetSelection(0, text.Length);
        _logger.Debug("Selected all text: {Length} characters", text.Length);
    }

    /// <summary>
    /// Clears the current selection without deleting text
    /// </summary>
    public new void ClearSelection()
    {
        var currentCaretIndex = CaretIndex;
        SetSelection(currentCaretIndex, 0);
        _logger.Debug("Selection cleared at caret position: {Position}", currentCaretIndex);
    }

    #endregion

    #region Clipboard Operations

    /// <summary>
    /// Copies selected text to clipboard
    /// </summary>
    public async void CopyToClipboard()
    {
        var selectedText = GetSelectedText();
        if (string.IsNullOrEmpty(selectedText))
        {
            _logger.Debug("No text selected to copy");
            return;
        }

        try
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
            {
                await clipboard.SetTextAsync(selectedText);
                _logger.Information("Copied {Length} characters to clipboard", selectedText.Length);
            }
            else
            {
                _logger.Warning("Clipboard not available");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to copy text to clipboard");
        }
    }

    /// <summary>
    /// Cuts selected text to clipboard
    /// </summary>
    public async void CutToClipboard()
    {
        var selectedText = GetSelectedText();
        if (string.IsNullOrEmpty(selectedText))
        {
            _logger.Debug("No text selected to cut");
            return;
        }

        try
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
            {
                await clipboard.SetTextAsync(selectedText);
                
                // Delete selected text
                var text = Text ?? string.Empty;
                var start = SelectionStart;
                var length = SelectionLength;
                
                var newText = text.Remove(start, length);
                Text = newText;
                MoveCaretTo(start);
                
                _logger.Information("Cut {Length} characters to clipboard", selectedText.Length);
            }
            else
            {
                _logger.Warning("Clipboard not available");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to cut text to clipboard");
        }
    }

    /// <summary>
    /// Pastes text from clipboard at current cursor position
    /// </summary>
    public async void PasteFromClipboard()
    {
        try
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
            {
#pragma warning disable CS0618 // Using GetTextAsync for simplicity
                var clipboardText = await clipboard.GetTextAsync() ?? string.Empty;
#pragma warning restore CS0618
                if (!string.IsNullOrEmpty(clipboardText))
                {
                    var text = Text ?? string.Empty;
                    var start = SelectionStart;
                    var length = SelectionLength;
                    
                    // Replace selected text or insert at cursor
                    var beforeSelection = text.Substring(0, start);
                    var afterSelection = start + length < text.Length 
                        ? text.Substring(start + length) 
                        : string.Empty;
                    
                    var newText = beforeSelection + clipboardText + afterSelection;
                    Text = newText;
                    
                    // Move caret to end of pasted text
                    MoveCaretTo(start + clipboardText.Length);
                    
                    _logger.Information("Pasted {Length} characters from clipboard", clipboardText.Length);
                }
                else
                {
                    _logger.Debug("Clipboard is empty");
                }
            }
            else
            {
                _logger.Warning("Clipboard not available");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to paste text from clipboard");
        }
    }

    #endregion

    #region Event Handling

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // Monitor caret index changes for visual feedback
        if (change.Property == CaretIndexProperty)
        {
            var caretIndex = (int)(change.NewValue ?? 0);
            CursorPositionChanged?.Invoke(this, new CursorPositionChangedEventArgs(caretIndex));
        }

        // Monitor selection changes
        if (change.Property == SelectionStartProperty || change.Property == SelectionEndProperty)
        {
            var selection = GetSelection();
            SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(selection));
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        // Handle keyboard shortcuts
        if (e.KeyModifiers == KeyModifiers.Control)
        {
            switch (e.Key)
            {
                case Key.C:
                    CopyToClipboard();
                    e.Handled = true;
                    return;
                case Key.X:
                    CutToClipboard();
                    e.Handled = true;
                    return;
                case Key.V:
                    PasteFromClipboard();
                    e.Handled = true;
                    return;
                case Key.A:
                    SelectAll();
                    e.Handled = true;
                    return;
            }
        }

        base.OnKeyDown(e);
    }

    #endregion
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

/// <summary>
/// Event arguments for selection changes
/// </summary>
public class SelectionChangedEventArgs : EventArgs
{
    public TextSelection Selection { get; }

    public SelectionChangedEventArgs(TextSelection selection)
    {
        Selection = selection ?? throw new ArgumentNullException(nameof(selection));
    }
}
