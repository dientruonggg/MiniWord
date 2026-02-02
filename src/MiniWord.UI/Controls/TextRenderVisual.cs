using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using MiniWord.Core.Models;
using MiniWord.UI.Services;
using System.Collections.Generic;

namespace MiniWord.UI.Controls;

/// <summary>
/// Custom visual control for rendering text lines using TextRenderer
/// This control demonstrates baseline alignment and proper line height calculation
/// </summary>
public class TextRenderVisual : Control
{
    private readonly TextRenderer _renderer;
    private readonly List<TextLine> _textLines;
    private readonly double _startX;
    private readonly double _startY;

    public TextRenderVisual(TextRenderer renderer, List<TextLine> textLines, double startX, double startY)
    {
        _renderer = renderer;
        _textLines = textLines;
        _startX = startX;
        _startY = startY;

        // Set the control size to fit the rendered content
        Width = 800; // Will be within A4 canvas
        Height = CalculateRequiredHeight();
    }

    private double CalculateRequiredHeight()
    {
        return _textLines.Count * _renderer.LineHeight;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        // Render all text lines using TextRenderer
        _renderer.RenderTextLines(context, _textLines, _startX, _startY, Brushes.Black);
    }
}
