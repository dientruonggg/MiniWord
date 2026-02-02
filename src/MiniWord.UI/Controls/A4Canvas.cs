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

namespace MiniWord.UI.Controls;

/// <summary>
/// Custom control representing an A4 paper canvas with margin visualization
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
    private readonly List<Line> _marginLines = new();
    private TextRenderer? _textRenderer;
    private TextFlowEngine? _textFlowEngine;
    
    // A4 dimensions at 96 DPI
    private const double A4_WIDTH = 794;
    private const double A4_HEIGHT = 1123;
    
    // Current margins (default: 1 inch = 96px)
    private double _leftMargin = 96;
    private double _topMargin = 96;
    private double _rightMargin = 96;
    private double _bottomMargin = 96;
    
    public A4Canvas()
    {
        _logger = Log.ForContext<A4Canvas>();
        
        // Initialize text rendering pipeline
        _textRenderer = new TextRenderer(_logger, fontFamily: new FontFamily("Times New Roman"), fontSize: 12);
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

        // Add margin lines and render canvas to the container
        textBoxContainer.Children.Add(_marginCanvas);
        textBoxContainer.Children.Add(_renderCanvas);
        
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
}
