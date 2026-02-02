using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using MiniWord.Core.Models;
using Serilog;
using System;

namespace MiniWord.UI.Controls;

/// <summary>
/// Horizontal ruler control showing measurements and margin indicators
/// </summary>
public partial class RulerControl : UserControl
{
    private readonly ILogger _logger;
    private Canvas _rulerCanvas = null!;
    private DocumentMargins _currentMargins;
    
    private const double RULER_HEIGHT = 30;
    private const double A4_WIDTH = 794;
    private const double PIXELS_PER_CM = 37.8; // 96 DPI / 2.54 cm per inch
    
    /// <summary>
    /// Event raised when margin is changed by dragging
    /// </summary>
    public event EventHandler<MarginsChangedEventArgs>? MarginChanged;

    public RulerControl()
    {
        _logger = Log.ForContext<RulerControl>();
        _currentMargins = new DocumentMargins(); // Default margins
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        _logger.Information("Initializing RulerControl");

        Height = RULER_HEIGHT;
        Background = new SolidColorBrush(Color.FromRgb(250, 250, 250));
        BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200));
        BorderThickness = new Thickness(0, 0, 0, 1);

        _rulerCanvas = new Canvas
        {
            Width = A4_WIDTH + 100,
            Height = RULER_HEIGHT,
            Background = Brushes.Transparent
        };

        DrawRuler();
        DrawMarginIndicators();

        Content = _rulerCanvas;

        _logger.Information("RulerControl initialized");
    }

    private void DrawRuler()
    {
        const double startX = 50; // Offset to align with paper

        // Draw ruler marks for centimeters
        for (int cm = 0; cm <= 21; cm++) // 21 cm for A4 width
        {
            double x = startX + (cm * PIXELS_PER_CM);
            
            // Major tick (centimeter)
            var tick = new Line
            {
                StartPoint = new Point(x, RULER_HEIGHT - 10),
                EndPoint = new Point(x, RULER_HEIGHT),
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };
            _rulerCanvas.Children.Add(tick);

            // Label every 2 cm
            if (cm % 2 == 0)
            {
                var label = new TextBlock
                {
                    Text = cm.ToString(),
                    FontSize = 9,
                    Foreground = Brushes.Black
                };
                Canvas.SetLeft(label, x - 5);
                Canvas.SetTop(label, 5);
                _rulerCanvas.Children.Add(label);
            }

            // Minor ticks (millimeters)
            if (cm < 21)
            {
                for (int mm = 1; mm < 10; mm++)
                {
                    double mmX = x + (mm * PIXELS_PER_CM / 10);
                    var minorTick = new Line
                    {
                        StartPoint = new Point(mmX, RULER_HEIGHT - 5),
                        EndPoint = new Point(mmX, RULER_HEIGHT),
                        Stroke = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
                        StrokeThickness = 0.5
                    };
                    _rulerCanvas.Children.Add(minorTick);
                }
            }
        }
    }

    private void DrawMarginIndicators()
    {
        const double startX = 50;

        // Left margin indicator
        var leftMarginLine = new Line
        {
            StartPoint = new Point(startX + _currentMargins.Left, 0),
            EndPoint = new Point(startX + _currentMargins.Left, RULER_HEIGHT),
            Stroke = new SolidColorBrush(Color.FromRgb(33, 150, 243)), // Blue
            StrokeThickness = 2
        };
        _rulerCanvas.Children.Add(leftMarginLine);

        // Right margin indicator
        var rightMarginLine = new Line
        {
            StartPoint = new Point(startX + A4_WIDTH - _currentMargins.Right, 0),
            EndPoint = new Point(startX + A4_WIDTH - _currentMargins.Right, RULER_HEIGHT),
            Stroke = new SolidColorBrush(Color.FromRgb(33, 150, 243)), // Blue
            StrokeThickness = 2
        };
        _rulerCanvas.Children.Add(rightMarginLine);

        _logger.Debug("Margin indicators drawn at Left={Left}px, Right={Right}px from right edge",
            _currentMargins.Left, _currentMargins.Right);
    }

    /// <summary>
    /// Updates the margin indicators
    /// </summary>
    public void UpdateMargins(DocumentMargins margins)
    {
        _logger.Information("Updating ruler margins: {Margins}", margins);
        _currentMargins = margins;
        
        // Redraw
        _rulerCanvas.Children.Clear();
        DrawRuler();
        DrawMarginIndicators();
    }
}
