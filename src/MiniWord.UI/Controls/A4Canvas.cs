using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using MiniWord.Core.Models;
using MiniWord.Core.Services;
using MiniWord.UI.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiniWord.UI.Controls;

/// <summary>
/// Custom control representing an A4 paper canvas with margin visualization
/// Enhanced with virtual rendering for performance optimization
/// </summary>
public partial class A4Canvas : UserControl
{
    private readonly ILogger _logger;
    private Canvas _paperCanvas = null!;
    private Border _paperBorder = null!;
    private ScrollViewer _scrollViewer = null!;
    private RichTextEditor _editorTextBox = null!;
    private Canvas _marginCanvas = null!;
    private Canvas _renderCanvas = null!;
    private Canvas _highlightCanvas = null!;
    private readonly List<Line> _marginLines = new();
    private TextRenderer? _textRenderer;
    private TextFlowEngine? _textFlowEngine;
    private A4Document? _document;
    
    // Search result highlighting (P5.2)
    private readonly List<Rectangle> _highlightRectangles = new();
    private TextRange? _currentHighlight;
    
    // Virtual rendering cache
    private readonly Dictionary<int, Control> _pageRenderCache = new();
    private int _lastVisibleStartPage = -1;
    private int _lastVisibleEndPage = -1;
    
    // A4 dimensions at 96 DPI
    private const double A4_WIDTH = 794;
    private const double A4_HEIGHT = 1123;
    
    // Page spacing for multi-page view
    private const double PAGE_SPACING = 20;
    
    // Current margins (default: 1 inch = 96px)
    private double _leftMargin = 96;
    private double _topMargin = 96;
    private double _rightMargin = 96;
    private double _bottomMargin = 96;
    
    public A4Canvas()
    {
        _logger = Log.ForContext<A4Canvas>();
        
        // Initialize text rendering pipeline with fallback font family
        _textRenderer = new TextRenderer(_logger, fontFamily: new FontFamily("Times New Roman, serif"), fontSize: 12);
        _textFlowEngine = new TextFlowEngine(_logger);
        
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        _logger.Information("Initializing A4Canvas control");

        // Create scroll viewer for the canvas
        _scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Background = new SolidColorBrush(Color.FromRgb(240, 240, 240))
        };

        // Create the canvas
        _paperCanvas = new Canvas
        {
            Width = A4_WIDTH + 100, // Add padding
            Height = A4_HEIGHT + 100,
            Background = new SolidColorBrush(Color.FromRgb(240, 240, 240))
        };

        // Create the A4 paper border (white background with shadow effect)
        _paperBorder = new Border
        {
            Width = A4_WIDTH,
            Height = A4_HEIGHT,
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
            BorderThickness = new Thickness(1),
            BoxShadow = new BoxShadows(new BoxShadow
            {
                OffsetX = 2,
                OffsetY = 2,
                Blur = 8,
                Color = Color.FromArgb(100, 0, 0, 0)
            })
        };

        // Position the paper in the center with padding
        Canvas.SetLeft(_paperBorder, 50);
        Canvas.SetTop(_paperBorder, 50);

        // Create the rich text editor with default margins (1 inch = 96px)
        _editorTextBox = new RichTextEditor
        {
            Width = A4_WIDTH - 192, // 96px left + 96px right margin
            Height = A4_HEIGHT - 192, // 96px top + 96px bottom margin
        };

        // Subscribe to cursor position changes for visual feedback
        _editorTextBox.CursorPositionChanged += OnCursorPositionChanged;

        // Create a container for the text box and margin visualization
        var textBoxContainer = new Canvas
        {
            Width = A4_WIDTH,
            Height = A4_HEIGHT
        };

        // Create canvas for margin lines (drawn behind text)
        _marginCanvas = new Canvas
        {
            Width = A4_WIDTH,
            Height = A4_HEIGHT
        };

        // Create canvas for text rendering (above margin lines, below editor)
        _renderCanvas = new Canvas
        {
            Width = A4_WIDTH,
            Height = A4_HEIGHT
        };

        // Create canvas for search result highlighting (P5.2) - above text, below editor
        _highlightCanvas = new Canvas
        {
            Width = A4_WIDTH,
            Height = A4_HEIGHT
        };

        // Add margin lines, render canvas, and highlight canvas to the container
        textBoxContainer.Children.Add(_marginCanvas);
        textBoxContainer.Children.Add(_renderCanvas);
        textBoxContainer.Children.Add(_highlightCanvas);
        
        // Position text box with margins
        Canvas.SetLeft(_editorTextBox, 96); // Left margin
        Canvas.SetTop(_editorTextBox, 96);  // Top margin

        textBoxContainer.Children.Add(_editorTextBox);
        
        // Draw initial margin indicators
        DrawMarginIndicators();
        
        _paperBorder.Child = textBoxContainer;

        _paperCanvas.Children.Add(_paperBorder);
        _scrollViewer.Content = _paperCanvas;

        Content = _scrollViewer;

        _logger.Information("A4Canvas initialized with dimensions {Width}x{Height}px", A4_WIDTH, A4_HEIGHT);
        
        // Subscribe to scroll events for viewport optimization
        _scrollViewer.ScrollChanged += OnScrollChanged;
    }

    /// <summary>
    /// Updates the margins of the text editor
    /// </summary>
    public void UpdateMargins(DocumentMargins margins)
    {
        _logger.Information("Updating margins: {Margins}", margins);

        if (_editorTextBox == null) return;

        // Store current margins
        _leftMargin = margins.Left;
        _topMargin = margins.Top;
        _rightMargin = margins.Right;
        _bottomMargin = margins.Bottom;

        // Update text box size based on new margins
        _editorTextBox.Width = A4_WIDTH - margins.TotalHorizontal;
        _editorTextBox.Height = A4_HEIGHT - margins.TotalVertical;

        // Update text box position
        Canvas.SetLeft(_editorTextBox, margins.Left);
        Canvas.SetTop(_editorTextBox, margins.Top);

        // Redraw margin indicators
        DrawMarginIndicators();

        _logger.Debug("Text box dimensions updated: {Width}x{Height}px at position ({Left}, {Top})",
            _editorTextBox.Width, _editorTextBox.Height, margins.Left, margins.Top);
    }

    /// <summary>
    /// Draws visual indicators for document margins
    /// </summary>
    private void DrawMarginIndicators()
    {
        // Clear existing margin lines
        _marginLines.Clear();
        _marginCanvas.Children.Clear();

        var marginBrush = new SolidColorBrush(Color.FromRgb(180, 180, 180)); // Light gray
        var dashedPen = new Pen(marginBrush, 1)
        {
            DashStyle = new DashStyle(new double[] { 4, 4 }, 0)
        };

        // Left margin line
        var leftLine = new Line
        {
            StartPoint = new Point(_leftMargin, 0),
            EndPoint = new Point(_leftMargin, A4_HEIGHT),
            Stroke = marginBrush,
            StrokeThickness = 1,
            StrokeDashArray = new AvaloniaList<double>(4, 4)
        };
        _marginLines.Add(leftLine);
        _marginCanvas.Children.Add(leftLine);

        // Right margin line
        var rightLine = new Line
        {
            StartPoint = new Point(A4_WIDTH - _rightMargin, 0),
            EndPoint = new Point(A4_WIDTH - _rightMargin, A4_HEIGHT),
            Stroke = marginBrush,
            StrokeThickness = 1,
            StrokeDashArray = new AvaloniaList<double>(4, 4)
        };
        _marginLines.Add(rightLine);
        _marginCanvas.Children.Add(rightLine);

        // Top margin line
        var topLine = new Line
        {
            StartPoint = new Point(0, _topMargin),
            EndPoint = new Point(A4_WIDTH, _topMargin),
            Stroke = marginBrush,
            StrokeThickness = 1,
            StrokeDashArray = new AvaloniaList<double>(4, 4)
        };
        _marginLines.Add(topLine);
        _marginCanvas.Children.Add(topLine);

        // Bottom margin line
        var bottomLine = new Line
        {
            StartPoint = new Point(0, A4_HEIGHT - _bottomMargin),
            EndPoint = new Point(A4_WIDTH, A4_HEIGHT - _bottomMargin),
            Stroke = marginBrush,
            StrokeThickness = 1,
            StrokeDashArray = new AvaloniaList<double>(4, 4)
        };
        _marginLines.Add(bottomLine);
        _marginCanvas.Children.Add(bottomLine);

        _logger.Debug("Margin indicators drawn at: L={Left}, R={Right}, T={Top}, B={Bottom}",
            _leftMargin, _rightMargin, _topMargin, _bottomMargin);
    }

    /// <summary>
    /// Handles cursor position changes to provide visual feedback
    /// </summary>
    private void OnCursorPositionChanged(object? sender, CursorPositionChangedEventArgs e)
    {
        // Implement visual feedback when text approaches margins
        // This provides a subtle indication to the user about margin proximity
        
        // Get the current text and calculate approximate position
        var text = _editorTextBox.Text ?? string.Empty;
        var caretIndex = e.CaretIndex;
        
        // Calculate line position (simplified - could be enhanced with actual text metrics)
        var textBeforeCaret = caretIndex < text.Length ? text.Substring(0, caretIndex) : text;
        var lastNewlineIndex = textBeforeCaret.LastIndexOf('\n');
        var lineStartIndex = lastNewlineIndex >= 0 ? lastNewlineIndex + 1 : 0;
        var currentLineLength = caretIndex - lineStartIndex;
        
        // Estimate horizontal position based on character count
        // This is a simplified approach; real implementation would use font metrics
        var estimatedCharWidth = _editorTextBox.FontSize * 0.6; // Rough estimate
        var estimatedXPosition = currentLineLength * estimatedCharWidth;
        
        // Calculate proximity to margins
        var textAreaWidth = _editorTextBox.Width;
        var proximityThreshold = 50; // pixels from edge
        
        // Determine if near margins
        var isNearLeftMargin = estimatedXPosition < proximityThreshold;
        var isNearRightMargin = estimatedXPosition > (textAreaWidth - proximityThreshold);
        
        // Update margin line appearance based on proximity
        if (isNearLeftMargin || isNearRightMargin)
        {
            // Highlight margins when text is nearby
            var highlightBrush = new SolidColorBrush(Color.FromRgb(100, 150, 200)); // Light blue
            
            if (isNearLeftMargin && _marginLines.Count > 0)
            {
                _marginLines[0].Stroke = highlightBrush;
            }
            
            if (isNearRightMargin && _marginLines.Count > 1)
            {
                _marginLines[1].Stroke = highlightBrush;
            }
            
            _logger.Debug("Cursor near margin at position: {Position}, NearLeft: {NearLeft}, NearRight: {NearRight}",
                e.CaretIndex, isNearLeftMargin, isNearRightMargin);
        }
        else
        {
            // Reset to default color when not near margins
            var defaultBrush = new SolidColorBrush(Color.FromRgb(180, 180, 180));
            foreach (var line in _marginLines)
            {
                line.Stroke = defaultBrush;
            }
        }
    }

    /// <summary>
    /// Gets or sets the text content
    /// </summary>
    public string Text
    {
        get => _editorTextBox?.Text ?? string.Empty;
        set
        {
            if (_editorTextBox != null)
            {
                _editorTextBox.Text = value;
            }
        }
    }

    #region Cursor and Selection Management

    /// <summary>
    /// Gets the current selection start position (WinForms compatibility)
    /// </summary>
    public int SelectionStart => _editorTextBox?.SelectionStart ?? 0;

    /// <summary>
    /// Gets the current selection length (WinForms compatibility)
    /// </summary>
    public int SelectionLength => _editorTextBox?.SelectionLength ?? 0;

    /// <summary>
    /// Gets the currently selected text
    /// </summary>
    public string SelectedText => _editorTextBox?.GetSelectedText() ?? string.Empty;

    /// <summary>
    /// Gets the current text selection
    /// </summary>
    public TextSelection GetSelection()
    {
        return _editorTextBox?.GetSelection() ?? TextSelection.Empty();
    }

    /// <summary>
    /// Sets the text selection
    /// </summary>
    /// <param name="start">Start position</param>
    /// <param name="length">Selection length</param>
    public void SetSelection(int start, int length)
    {
        _editorTextBox?.SetSelection(start, length);
    }

    /// <summary>
    /// Moves the caret to the specified position
    /// </summary>
    /// <param name="position">Target position</param>
    public void MoveCaretTo(int position)
    {
        _editorTextBox?.MoveCaretTo(position);
    }

    /// <summary>
    /// Selects all text
    /// </summary>
    public void SelectAll()
    {
        _editorTextBox?.SelectAll();
    }

    /// <summary>
    /// Clears the current selection
    /// </summary>
    public void ClearSelection()
    {
        _editorTextBox?.ClearSelection();
    }

    /// <summary>
    /// Copies selected text to clipboard
    /// </summary>
    public void Copy()
    {
        _ = _editorTextBox?.CopyToClipboardAsync();
    }

    /// <summary>
    /// Cuts selected text to clipboard
    /// </summary>
    public void Cut()
    {
        _ = _editorTextBox?.CutToClipboardAsync();
    }

    /// <summary>
    /// Pastes text from clipboard
    /// </summary>
    public void Paste()
    {
        _ = _editorTextBox?.PasteFromClipboardAsync();
    }

    /// <summary>
    /// Gets the current caret position
    /// </summary>
    public int CaretPosition => _editorTextBox?.CaretIndex ?? 0;

    #endregion

    #region Text Formatting (P5.3)

    /// <summary>
    /// Applies or removes formatting to the currently selected text
    /// </summary>
    /// <param name="formatting">The formatting to apply</param>
    /// <param name="add">True to add formatting, false to remove it</param>
    public void ApplyFormatting(TextFormatting formatting, bool add = true)
    {
        if (_document == null)
        {
            _logger.Warning("Cannot apply formatting: document not initialized");
            return;
        }

        var selectionStart = SelectionStart;
        var selectionLength = SelectionLength;

        if (selectionLength == 0)
        {
            _logger.Debug("No text selected, formatting not applied");
            return;
        }

        _logger.Information("Applying formatting {Formatting} to range [{Start}, {End}), add={Add}",
            formatting, selectionStart, selectionStart + selectionLength, add);

        try
        {
            if (add)
            {
                // Add new formatting span
                var newSpan = new FormattingSpan(selectionStart, selectionLength, formatting);
                
                // Merge with existing spans or add as new
                MergeFormattingSpan(newSpan);
            }
            else
            {
                // Remove formatting from the selected range
                RemoveFormattingFromRange(selectionStart, selectionLength, formatting);
            }

            // Trigger re-render
            RefreshTextRendering();

            _logger.Information("Formatting applied successfully");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to apply formatting");
        }
    }

    /// <summary>
    /// Toggles bold formatting on the selected text
    /// </summary>
    public void ToggleBold()
    {
        var hasBold = HasFormatting(TextFormatting.Bold);
        ApplyFormatting(TextFormatting.Bold, !hasBold);
    }

    /// <summary>
    /// Toggles italic formatting on the selected text
    /// </summary>
    public void ToggleItalic()
    {
        var hasItalic = HasFormatting(TextFormatting.Italic);
        ApplyFormatting(TextFormatting.Italic, !hasItalic);
    }

    /// <summary>
    /// Toggles underline formatting on the selected text
    /// </summary>
    public void ToggleUnderline()
    {
        var hasUnderline = HasFormatting(TextFormatting.Underline);
        ApplyFormatting(TextFormatting.Underline, !hasUnderline);
    }

    /// <summary>
    /// Checks if the current selection has the specified formatting
    /// </summary>
    /// <param name="formatting">The formatting to check</param>
    /// <returns>True if all selected text has this formatting</returns>
    public bool HasFormatting(TextFormatting formatting)
    {
        if (_document == null || SelectionLength == 0)
        {
            return false;
        }

        var selectionStart = SelectionStart;
        var selectionEnd = selectionStart + SelectionLength;

        // Check if any span in the selection has this formatting
        foreach (var span in _document.FormattingSpans)
        {
            if (span.StartIndex < selectionEnd && span.EndIndex > selectionStart)
            {
                if (span.Formatting.HasFlag(formatting))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Merges a new formatting span with existing spans
    /// </summary>
    private void MergeFormattingSpan(FormattingSpan newSpan)
    {
        if (_document == null) return;

        // Find overlapping spans with same formatting
        var overlappingSpans = _document.FormattingSpans
            .Where(s => s.OverlapsWith(newSpan) && s.Formatting == newSpan.Formatting)
            .ToList();

        if (overlappingSpans.Count == 0)
        {
            // No overlap, just add the new span
            _document.FormattingSpans.Add(newSpan);
        }
        else
        {
            // Merge overlapping spans
            var minStart = Math.Min(newSpan.StartIndex, overlappingSpans.Min(s => s.StartIndex));
            var maxEnd = Math.Max(newSpan.EndIndex, overlappingSpans.Max(s => s.EndIndex));

            // Remove old spans
            foreach (var span in overlappingSpans)
            {
                _document.FormattingSpans.Remove(span);
            }

            // Add merged span
            _document.FormattingSpans.Add(new FormattingSpan(minStart, maxEnd - minStart, newSpan.Formatting));
        }

        _logger.Debug("Formatting span merged: {Span}", newSpan);
    }

    /// <summary>
    /// Removes formatting from a specific range
    /// </summary>
    private void RemoveFormattingFromRange(int start, int length, TextFormatting formatting)
    {
        if (_document == null) return;

        var end = start + length;
        var spansToModify = _document.FormattingSpans
            .Where(s => s.StartIndex < end && s.EndIndex > start && s.Formatting.HasFlag(formatting))
            .ToList();

        foreach (var span in spansToModify)
        {
            _document.FormattingSpans.Remove(span);

            // Remove the specific formatting flag
            var newFormatting = span.Formatting & ~formatting;

            // Split span if needed
            if (span.StartIndex < start)
            {
                // Keep the part before the selection
                _document.FormattingSpans.Add(new FormattingSpan(span.StartIndex, start - span.StartIndex, span.Formatting));
            }

            if (span.EndIndex > end)
            {
                // Keep the part after the selection
                _document.FormattingSpans.Add(new FormattingSpan(end, span.EndIndex - end, span.Formatting));
            }

            // Add the middle part with updated formatting (if it still has formatting)
            if (newFormatting != TextFormatting.None)
            {
                var middleStart = Math.Max(span.StartIndex, start);
                var middleEnd = Math.Min(span.EndIndex, end);
                _document.FormattingSpans.Add(new FormattingSpan(middleStart, middleEnd - middleStart, newFormatting));
            }
        }

        _logger.Debug("Formatting removed from range [{Start}, {End})", start, end);
    }

    /// <summary>
    /// Refreshes the text rendering with current formatting
    /// </summary>
    private void RefreshTextRendering()
    {
        // Re-render the text with updated formatting
        var text = Text;
        if (!string.IsNullOrEmpty(text))
        {
            RenderTextWithPipeline(text);
        }
    }

    #endregion

    #region Text Rendering Pipeline

    /// <summary>
    /// Renders text using TextFlowEngine and TextRenderer
    /// This demonstrates the P2.2 text rendering pipeline integration
    /// </summary>
    public void RenderTextWithPipeline(string text)
    {
        if (_textRenderer == null || _textFlowEngine == null)
        {
            _logger.Warning("Text rendering pipeline not initialized");
            return;
        }

        _logger.Information("Rendering text with pipeline: {Length} characters", text?.Length ?? 0);

        // Clear previous rendering
        _renderCanvas.Children.Clear();

        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        try
        {
            // Calculate available width for text (paper width - margins)
            var availableWidth = A4_WIDTH - _leftMargin - _rightMargin;

            // Use TextFlowEngine to calculate line breaks with TextRenderer's measurement function
            var measureFunc = _textRenderer.GetMeasurementFunction();
            var textLines = _textFlowEngine.CalculateLineBreaks(text, availableWidth, measureFunc);

            _logger.Information("Text flow calculated: {LineCount} lines", textLines.Count);

            // Create a visual element to render the text
            var textVisual = new TextRenderVisual(_textRenderer, textLines, _leftMargin, _topMargin);
            
            // Add to render canvas
            _renderCanvas.Children.Add(textVisual);

            _logger.Information("Text rendering complete");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to render text with pipeline");
        }
    }

    /// <summary>
    /// Gets the current TextRenderer instance
    /// </summary>
    public TextRenderer? GetTextRenderer()
    {
        return _textRenderer;
    }

    /// <summary>
    /// Gets the current TextFlowEngine instance
    /// </summary>
    public TextFlowEngine? GetTextFlowEngine()
    {
        return _textFlowEngine;
    }

    #endregion

    #region Scrolling and Viewport Management (P2.4)

    /// <summary>
    /// Sets the document for this canvas
    /// </summary>
    public void SetDocument(A4Document? document)
    {
        _document = document;
        if (_document != null)
        {
            _logger.Information("Document set with {PageCount} pages", _document.PageCount);
            UpdateCanvasForMultiplePages();
        }
    }

    /// <summary>
    /// Gets the current document
    /// </summary>
    public A4Document? GetDocument()
    {
        return _document;
    }

    /// <summary>
    /// Updates canvas size to accommodate multiple pages
    /// </summary>
    private void UpdateCanvasForMultiplePages()
    {
        if (_document == null) return;

        // Calculate total height for all pages with spacing
        var totalHeight = (_document.PageCount * A4_HEIGHT) + 
                         ((_document.PageCount - 1) * PAGE_SPACING) + 
                         100; // Padding

        _paperCanvas.Height = totalHeight;
        _logger.Debug("Canvas height updated for {PageCount} pages: {Height}px", 
            _document.PageCount, totalHeight);
    }

    /// <summary>
    /// Handles scroll events for viewport optimization with virtual rendering
    /// Only renders pages that are visible in the current viewport
    /// </summary>
    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        // Log scroll position for debugging
        _logger.Debug("Scroll changed - Offset: {Offset}, Extent: {Extent}, Viewport: {Viewport}",
            e.OffsetDelta, e.ExtentDelta, e.ViewportDelta);

        // Implement virtual rendering: only render visible pages
        UpdateVisiblePages();
    }

    /// <summary>
    /// Updates which pages are rendered based on the current viewport
    /// This implements virtual rendering for performance optimization with large documents
    /// </summary>
    private void UpdateVisiblePages()
    {
        if (_document == null || _document.PageCount == 0)
        {
            return;
        }

        // Calculate which pages are currently visible in the viewport
        var (startPage, endPage) = GetVisiblePageRange();

        // Check if the visible range has changed
        if (startPage == _lastVisibleStartPage && endPage == _lastVisibleEndPage)
        {
            return; // No change, nothing to update
        }

        _logger.Information("Visible page range changed from [{OldStart}, {OldEnd}] to [{NewStart}, {NewEnd}]",
            _lastVisibleStartPage, _lastVisibleEndPage, startPage, endPage);

        // Remove pages that are no longer visible from the render canvas
        for (int pageIndex = 0; pageIndex < _document.PageCount; pageIndex++)
        {
            if (pageIndex < startPage || pageIndex > endPage)
            {
                // Page is outside visible range - remove from render if present
                if (_pageRenderCache.TryGetValue(pageIndex, out var cachedControl))
                {
                    if (_renderCanvas.Children.Contains(cachedControl))
                    {
                        _renderCanvas.Children.Remove(cachedControl);
                        _logger.Debug("Removed page {Page} from render canvas (out of viewport)", pageIndex + 1);
                    }
                }
            }
        }

        // Add or restore pages that are now visible
        for (int pageIndex = startPage; pageIndex <= endPage; pageIndex++)
        {
            if (_pageRenderCache.TryGetValue(pageIndex, out var cachedControl))
            {
                // Page is cached, restore to canvas if not already there
                if (!_renderCanvas.Children.Contains(cachedControl))
                {
                    _renderCanvas.Children.Add(cachedControl);
                    _logger.Debug("Restored cached page {Page} to render canvas", pageIndex + 1);
                }
            }
            else
            {
                // Page is not cached, would render it here if we had page-specific content
                // For now, we just log that the page should be rendered
                _logger.Debug("Page {Page} is visible and should be rendered", pageIndex + 1);
            }
        }

        _lastVisibleStartPage = startPage;
        _lastVisibleEndPage = endPage;
    }

    /// <summary>
    /// Calculates which pages are currently visible in the viewport
    /// Returns a tuple of (startPage, endPage) indices (0-based)
    /// </summary>
    public (int startPage, int endPage) GetVisiblePageRange()
    {
        if (_document == null || _document.PageCount == 0)
        {
            return (0, 0);
        }

        var scrollOffset = _scrollViewer.Offset.Y;
        var viewportHeight = _scrollViewer.Viewport.Height;

        // Calculate which pages are visible
        // Each page has height A4_HEIGHT + PAGE_SPACING (except last page)
        var pageHeight = A4_HEIGHT + PAGE_SPACING;
        
        // Account for initial padding (50px)
        var contentOffset = scrollOffset - 50;
        
        // Calculate start page (page at top of viewport)
        var startPage = Math.Max(0, (int)(contentOffset / pageHeight));
        
        // Calculate end page (page at bottom of viewport)
        // Add a buffer page for smooth scrolling
        var endOffset = contentOffset + viewportHeight + pageHeight;
        var endPage = Math.Min(_document.PageCount - 1, (int)(endOffset / pageHeight));

        _logger.Debug("Visible page range: [{Start}, {End}] from scroll offset {Offset}px, viewport height {Height}px",
            startPage + 1, endPage + 1, scrollOffset, viewportHeight);

        return (startPage, endPage);
    }

    /// <summary>
    /// Clears the page render cache
    /// Call this when document content changes significantly
    /// </summary>
    public void ClearPageCache()
    {
        foreach (var cachedPage in _pageRenderCache.Values)
        {
            if (_renderCanvas.Children.Contains(cachedPage))
            {
                _renderCanvas.Children.Remove(cachedPage);
            }
        }
        
        _pageRenderCache.Clear();
        _lastVisibleStartPage = -1;
        _lastVisibleEndPage = -1;
        
        _logger.Information("Page render cache cleared");
    }

    /// <summary>
    /// Scrolls to a specific page
    /// </summary>
    /// <param name="pageIndex">Zero-based page index</param>
    public void ScrollToPage(int pageIndex)
    {
        if (_document == null || pageIndex < 0 || pageIndex >= _document.PageCount)
        {
            _logger.Warning("Invalid page index for scrolling: {PageIndex}", pageIndex);
            return;
        }

        // Calculate vertical offset for the page
        var offset = 50 + (pageIndex * (A4_HEIGHT + PAGE_SPACING));
        
        _logger.Information("Scrolling to page {PageNumber} at offset {Offset}px", 
            pageIndex + 1, offset);

        _scrollViewer.Offset = new Vector(_scrollViewer.Offset.X, offset);
    }

    /// <summary>
    /// Scrolls to the top of the document (first page)
    /// </summary>
    public void ScrollToTop()
    {
        _logger.Information("Scrolling to top of document");
        _scrollViewer.Offset = new Vector(0, 0);
    }

    /// <summary>
    /// Scrolls to the bottom of the document (last page)
    /// </summary>
    public void ScrollToBottom()
    {
        _logger.Information("Scrolling to bottom of document");
        var maxOffset = _scrollViewer.Extent.Height - _scrollViewer.Viewport.Height;
        _scrollViewer.Offset = new Vector(0, maxOffset);
    }

    /// <summary>
    /// Scrolls up by one page (viewport height)
    /// </summary>
    public void ScrollPageUp()
    {
        var newOffset = Math.Max(0, _scrollViewer.Offset.Y - _scrollViewer.Viewport.Height);
        _logger.Debug("Scrolling page up from {Current} to {New}", 
            _scrollViewer.Offset.Y, newOffset);
        _scrollViewer.Offset = new Vector(_scrollViewer.Offset.X, newOffset);
    }

    /// <summary>
    /// Scrolls down by one page (viewport height)
    /// </summary>
    public void ScrollPageDown()
    {
        var maxOffset = _scrollViewer.Extent.Height - _scrollViewer.Viewport.Height;
        var newOffset = Math.Min(maxOffset, _scrollViewer.Offset.Y + _scrollViewer.Viewport.Height);
        _logger.Debug("Scrolling page down from {Current} to {New}", 
            _scrollViewer.Offset.Y, newOffset);
        _scrollViewer.Offset = new Vector(_scrollViewer.Offset.X, newOffset);
    }

    /// <summary>
    /// Scrolls smoothly to a specific vertical offset
    /// </summary>
    /// <param name="targetOffset">Target vertical offset in pixels</param>
    public async void ScrollToOffsetSmooth(double targetOffset)
    {
        const int steps = 20;
        const int delayMs = 10;
        
        var currentOffset = _scrollViewer.Offset.Y;
        var delta = (targetOffset - currentOffset) / steps;

        _logger.Debug("Smooth scrolling from {Current} to {Target}", currentOffset, targetOffset);

        for (int i = 0; i < steps; i++)
        {
            currentOffset += delta;
            _scrollViewer.Offset = new Vector(_scrollViewer.Offset.X, currentOffset);
            await Task.Delay(delayMs);
        }

        // Ensure we end exactly at the target
        _scrollViewer.Offset = new Vector(_scrollViewer.Offset.X, targetOffset);
    }

    /// <summary>
    /// Gets the current scroll offset
    /// </summary>
    public Vector GetScrollOffset()
    {
        return _scrollViewer.Offset;
    }

    /// <summary>
    /// Gets the viewport size
    /// </summary>
    public Size GetViewportSize()
    {
        return _scrollViewer.Viewport;
    }

    /// <summary>
    /// Gets the currently visible page index based on scroll position
    /// </summary>
    public int GetVisiblePageIndex()
    {
        if (_document == null || _document.PageCount == 0) return 0;

        var scrollOffset = _scrollViewer.Offset.Y;
        var pageIndex = (int)((scrollOffset - 50) / (A4_HEIGHT + PAGE_SPACING));
        
        return Math.Max(0, Math.Min(pageIndex, _document.PageCount - 1));
    }

    #endregion

    #region Search Result Highlighting (P5.2)

    /// <summary>
    /// Highlights a text range in the editor
    /// </summary>
    /// <param name="range">The text range to highlight</param>
    public void HighlightSearchResult(TextRange range)
    {
        try
        {
            if (range == null || range.IsEmpty)
            {
                _logger.Warning("Cannot highlight empty or null range");
                return;
            }

            _logger.Information("Highlighting search result: {Range}", range);

            // Clear previous highlights
            ClearSearchHighlights();

            // Store current highlight
            _currentHighlight = range;

            // Set selection in the editor to the matched text
            _editorTextBox.SetSelection(range.Start, range.Length);

            // Calculate approximate position for visual highlight
            // This is a simplified implementation - could be enhanced with actual text metrics
            var text = _editorTextBox.Text ?? string.Empty;
            var textBeforeMatch = range.Start < text.Length ? text.Substring(0, range.Start) : text;
            
            // Count lines before the match
            var linesBefore = textBeforeMatch.Split('\n').Length - 1;
            
            // Estimate Y position (line height * line number)
            var estimatedLineHeight = _editorTextBox.FontSize * 1.5;
            var yPosition = linesBefore * estimatedLineHeight;

            // Get the text on the current line to estimate X position
            var lastNewlineIndex = textBeforeMatch.LastIndexOf('\n');
            var lineStartIndex = lastNewlineIndex >= 0 ? lastNewlineIndex + 1 : 0;
            var charsBeforeMatchOnLine = range.Start - lineStartIndex;
            
            // Estimate X position
            var estimatedCharWidth = _editorTextBox.FontSize * 0.6;
            var xPosition = charsBeforeMatchOnLine * estimatedCharWidth;

            // Calculate highlight width
            var matchText = range.End <= text.Length ? text.Substring(range.Start, range.Length) : string.Empty;
            var highlightWidth = matchText.Length * estimatedCharWidth;

            // Create highlight rectangle with semi-transparent yellow background
            var highlightRect = new Rectangle
            {
                Width = highlightWidth,
                Height = estimatedLineHeight,
                Fill = new SolidColorBrush(Color.FromArgb(100, 255, 255, 0)), // Semi-transparent yellow
                Stroke = new SolidColorBrush(Color.FromRgb(255, 200, 0)),
                StrokeThickness = 1
            };

            // Position the highlight rectangle
            Canvas.SetLeft(highlightRect, _leftMargin + xPosition);
            Canvas.SetTop(highlightRect, _topMargin + yPosition);

            // Add to highlight canvas
            _highlightCanvas.Children.Add(highlightRect);
            _highlightRectangles.Add(highlightRect);

            _logger.Debug("Search highlight added at position ({X}, {Y}) with width {Width}", 
                xPosition, yPosition, highlightWidth);

            // Scroll to make the highlight visible
            ScrollToHighlight(yPosition);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to highlight search result");
        }
    }

    /// <summary>
    /// Highlights multiple search results
    /// </summary>
    /// <param name="ranges">The text ranges to highlight</param>
    public void HighlightSearchResults(List<TextRange> ranges)
    {
        try
        {
            if (ranges == null || ranges.Count == 0)
            {
                _logger.Debug("No ranges to highlight");
                return;
            }

            _logger.Information("Highlighting {Count} search results", ranges.Count);

            // Clear previous highlights
            ClearSearchHighlights();

            var text = _editorTextBox.Text ?? string.Empty;
            var estimatedLineHeight = _editorTextBox.FontSize * 1.5;
            var estimatedCharWidth = _editorTextBox.FontSize * 0.6;

            foreach (var range in ranges)
            {
                if (range.IsEmpty) continue;

                // Calculate position for each highlight
                var textBeforeMatch = range.Start < text.Length ? text.Substring(0, range.Start) : text;
                var linesBefore = textBeforeMatch.Split('\n').Length - 1;
                var yPosition = linesBefore * estimatedLineHeight;

                var lastNewlineIndex = textBeforeMatch.LastIndexOf('\n');
                var lineStartIndex = lastNewlineIndex >= 0 ? lastNewlineIndex + 1 : 0;
                var charsBeforeMatchOnLine = range.Start - lineStartIndex;
                var xPosition = charsBeforeMatchOnLine * estimatedCharWidth;

                var matchText = range.End <= text.Length ? text.Substring(range.Start, range.Length) : string.Empty;
                var highlightWidth = matchText.Length * estimatedCharWidth;

                // Create highlight rectangle
                var highlightRect = new Rectangle
                {
                    Width = highlightWidth,
                    Height = estimatedLineHeight,
                    Fill = new SolidColorBrush(Color.FromArgb(80, 255, 255, 0)), // Lighter yellow for multiple highlights
                    Stroke = new SolidColorBrush(Color.FromRgb(255, 200, 0)),
                    StrokeThickness = 0.5
                };

                Canvas.SetLeft(highlightRect, _leftMargin + xPosition);
                Canvas.SetTop(highlightRect, _topMargin + yPosition);

                _highlightCanvas.Children.Add(highlightRect);
                _highlightRectangles.Add(highlightRect);
            }

            _logger.Debug("Added {Count} search highlights", _highlightRectangles.Count);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to highlight search results");
        }
    }

    /// <summary>
    /// Clears all search result highlights
    /// </summary>
    public void ClearSearchHighlights()
    {
        try
        {
            _logger.Debug("Clearing {Count} search highlights", _highlightRectangles.Count);

            foreach (var rect in _highlightRectangles)
            {
                _highlightCanvas.Children.Remove(rect);
            }

            _highlightRectangles.Clear();
            _currentHighlight = null;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to clear search highlights");
        }
    }

    /// <summary>
    /// Scrolls to make a highlight visible
    /// </summary>
    private void ScrollToHighlight(double yPosition)
    {
        try
        {
            var targetOffset = _topMargin + yPosition - (_scrollViewer.Viewport.Height / 2);
            targetOffset = Math.Max(0, targetOffset);

            _logger.Debug("Scrolling to highlight at Y position {Y}, target offset {Offset}", 
                yPosition, targetOffset);

            _scrollViewer.Offset = new Vector(_scrollViewer.Offset.X, targetOffset);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to scroll to highlight");
        }
    }

    /// <summary>
    /// Gets the current highlight range
    /// </summary>
    public TextRange? GetCurrentHighlight()
    {
        return _currentHighlight;
    }

    #endregion
}
