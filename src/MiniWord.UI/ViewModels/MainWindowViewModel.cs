using System.ComponentModel;
using System.Runtime.CompilerServices;
using MiniWord.Core.Models;
using MiniWord.Core.Services;
using Serilog;

namespace MiniWord.UI.ViewModels;

/// <summary>
/// ViewModel for MainWindow - demonstrates event/delegate pattern
/// </summary>
public class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly ILogger _logger;
    private readonly MarginCalculator _marginCalculator;
    private DocumentMargins _margins;
    private string _documentText;

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainWindowViewModel()
    {
        _logger = Log.ForContext<MainWindowViewModel>();
        _marginCalculator = new MarginCalculator(_logger);
        _margins = new DocumentMargins(); // Default 1 inch margins
        _documentText = string.Empty;

        _logger.Information("MainWindowViewModel initialized");
    }

    /// <summary>
    /// Document margins (in pixels)
    /// </summary>
    public DocumentMargins Margins
    {
        get => _margins;
        set
        {
            if (_margins != value)
            {
                var oldMargins = _margins;
                _margins = value;
                OnPropertyChanged();
                
                _logger.Information("Margins changed in ViewModel: {OldMargins} -> {NewMargins}",
                    oldMargins, value);
            }
        }
    }

    /// <summary>
    /// Document text content
    /// </summary>
    public string DocumentText
    {
        get => _documentText;
        set
        {
            if (_documentText != value)
            {
                _documentText = value;
                OnPropertyChanged();
                _logger.Debug("Document text changed (length: {Length})", value?.Length ?? 0);
            }
        }
    }

    /// <summary>
    /// Left margin in millimeters (for UI binding)
    /// </summary>
    public double LeftMarginMm
    {
        get => _marginCalculator.PixelsToMillimeters(Margins.Left);
        set
        {
            var pixels = _marginCalculator.MillimetersToPixels(value);
            var newMargins = new DocumentMargins(pixels, Margins.Right, Margins.Top, Margins.Bottom);
            Margins = newMargins;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Right margin in millimeters (for UI binding)
    /// </summary>
    public double RightMarginMm
    {
        get => _marginCalculator.PixelsToMillimeters(Margins.Right);
        set
        {
            var pixels = _marginCalculator.MillimetersToPixels(value);
            var newMargins = new DocumentMargins(Margins.Left, pixels, Margins.Top, Margins.Bottom);
            Margins = newMargins;
            OnPropertyChanged();
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
