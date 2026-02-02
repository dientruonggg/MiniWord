using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using MiniWord.Core.Models;
using Serilog;

namespace MiniWord.UI.Controls;

/// <summary>
/// Custom control representing an A4 paper canvas
/// </summary>
public partial class A4Canvas : UserControl
{
    private readonly ILogger _logger;
    private Canvas _paperCanvas = null!;
    private Border _paperBorder = null!;
    private ScrollViewer _scrollViewer = null!;
    private TextBox _editorTextBox = null!;
    
    // A4 dimensions at 96 DPI
    private const double A4_WIDTH = 794;
    private const double A4_HEIGHT = 1123;
    
    public A4Canvas()
    {
        _logger = Log.ForContext<A4Canvas>();
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

        // Create the text editor with default margins (1 inch = 96px)
        _editorTextBox = new TextBox
        {
            Width = A4_WIDTH - 192, // 96px left + 96px right margin
            Height = A4_HEIGHT - 192, // 96px top + 96px bottom margin
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            Padding = new Thickness(0),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            FontFamily = new FontFamily("Times New Roman"),
            FontSize = 12
        };

        // Create a container for the text box to handle margins
        var textBoxContainer = new Canvas
        {
            Width = A4_WIDTH,
            Height = A4_HEIGHT
        };

        // Position text box with margins
        Canvas.SetLeft(_editorTextBox, 96); // Left margin
        Canvas.SetTop(_editorTextBox, 96);  // Top margin

        textBoxContainer.Children.Add(_editorTextBox);
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

        // Update text box size based on new margins
        _editorTextBox.Width = A4_WIDTH - margins.TotalHorizontal;
        _editorTextBox.Height = A4_HEIGHT - margins.TotalVertical;

        // Update text box position
        Canvas.SetLeft(_editorTextBox, margins.Left);
        Canvas.SetTop(_editorTextBox, margins.Top);

        _logger.Debug("Text box dimensions updated: {Width}x{Height}px at position ({Left}, {Top})",
            _editorTextBox.Width, _editorTextBox.Height, margins.Left, margins.Top);
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
}
