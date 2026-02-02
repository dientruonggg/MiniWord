using MiniWord.Core.Exceptions;
using Serilog;
using System.ComponentModel;

namespace MiniWord.Core.Models;

/// <summary>
/// Represents an A4 document (210mm x 297mm)
/// At 96 DPI: 794 x 1123 pixels
/// </summary>
public class A4Document : INotifyPropertyChanged
{
    private readonly ILogger _logger;
    private string _content = string.Empty;
    private bool _isDirty = false;
    private int _currentPageIndex = 0;

    /// <summary>
    /// A4 width in pixels at 96 DPI (210mm)
    /// </summary>
    public const double A4_WIDTH_PX = 794;

    /// <summary>
    /// A4 height in pixels at 96 DPI (297mm)
    /// </summary>
    public const double A4_HEIGHT_PX = 1123;

    /// <summary>
    /// PropertyChanged event for INotifyPropertyChanged implementation
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Event raised when margins are changed
    /// </summary>
    public event EventHandler<MarginsChangedEventArgs>? MarginsChanged;

    /// <summary>
    /// Document margins
    /// </summary>
    public DocumentMargins Margins { get; private set; }

    /// <summary>
    /// Collection of pages in the document
    /// </summary>
    public List<Page> Pages { get; private set; } = new List<Page>();

    /// <summary>
    /// Document content
    /// </summary>
    public string Content 
    { 
        get => _content;
        set
        {
            if (_content != value)
            {
                _content = value;
                IsDirty = true;
                OnPropertyChanged(nameof(Content));
            }
        }
    }

    /// <summary>
    /// Indicates whether the document has unsaved changes
    /// </summary>
    public bool IsDirty
    {
        get => _isDirty;
        set
        {
            if (_isDirty != value)
            {
                _isDirty = value;
                OnPropertyChanged(nameof(IsDirty));
                _logger.Debug("Document dirty state changed to: {IsDirty}", _isDirty);
            }
        }
    }

    /// <summary>
    /// Current page index (0-based)
    /// </summary>
    public int CurrentPageIndex
    {
        get => _currentPageIndex;
        private set
        {
            if (_currentPageIndex != value)
            {
                _currentPageIndex = value;
                OnPropertyChanged(nameof(CurrentPageIndex));
                OnPropertyChanged(nameof(CurrentPageNumber));
            }
        }
    }

    /// <summary>
    /// Current page number (1-based, for display)
    /// </summary>
    public int CurrentPageNumber => CurrentPageIndex + 1;

    /// <summary>
    /// Total number of pages in the document
    /// </summary>
    public int PageCount => Pages.Count;

    /// <summary>
    /// Available width for text (paper width - margins)
    /// </summary>
    public double AvailableWidth => A4_WIDTH_PX - Margins.TotalHorizontal;

    /// <summary>
    /// Available height for text (paper height - margins)
    /// </summary>
    public double AvailableHeight => A4_HEIGHT_PX - Margins.TotalVertical;

    public A4Document(ILogger logger)
    {
        _logger = logger;
        Margins = new DocumentMargins();
        
        // Initialize with at least one page
        AddPage();
        
        _logger.Information("A4 Document created with dimensions {Width}x{Height}px, margins: {Margins}",
            A4_WIDTH_PX, A4_HEIGHT_PX, Margins);
    }

    /// <summary>
    /// Raises the PropertyChanged event
    /// </summary>
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Updates document margins and raises MarginsChanged event
    /// </summary>
    public void UpdateMargins(DocumentMargins newMargins)
    {
        if (newMargins.TotalHorizontal >= A4_WIDTH_PX)
        {
            var ex = new MarginException(
                $"Total horizontal margin ({newMargins.TotalHorizontal:F1}px) exceeds page width ({A4_WIDTH_PX}px)");
            _logger.Error(ex, "Invalid margins: Total horizontal margin {Total} exceeds page width {Width}",
                newMargins.TotalHorizontal, A4_WIDTH_PX);
            throw ex;
        }

        if (newMargins.TotalVertical >= A4_HEIGHT_PX)
        {
            var ex = new MarginException(
                $"Total vertical margin ({newMargins.TotalVertical:F1}px) exceeds page height ({A4_HEIGHT_PX}px)");
            _logger.Error(ex, "Invalid margins: Total vertical margin {Total} exceeds page height {Height}",
                newMargins.TotalVertical, A4_HEIGHT_PX);
            throw ex;
        }

        var oldMargins = Margins;
        Margins = newMargins;

        _logger.Information("Margins updated from {OldMargins} to {NewMargins}. Available width: {Width}px",
            oldMargins, newMargins, AvailableWidth);

        MarginsChanged?.Invoke(this, new MarginsChangedEventArgs(oldMargins, newMargins));
    }

    /// <summary>
    /// Validates that the given width is within document bounds
    /// </summary>
    public bool IsValidWidth(double width)
    {
        return width > 0 && width <= AvailableWidth;
    }

    #region Page Management Methods

    /// <summary>
    /// Adds a new page to the document
    /// </summary>
    public Page AddPage()
    {
        var pageNumber = Pages.Count + 1;
        var page = new Page(pageNumber);
        Pages.Add(page);
        
        IsDirty = true;
        OnPropertyChanged(nameof(PageCount));
        
        _logger.Information("Page {PageNumber} added to document. Total pages: {PageCount}", 
            pageNumber, PageCount);
        
        return page;
    }

    /// <summary>
    /// Adds a new page with content to the document
    /// </summary>
    public Page AddPage(string content)
    {
        var page = AddPage();
        page.Content = content;
        return page;
    }

    /// <summary>
    /// Removes a page from the document at the specified index
    /// </summary>
    public bool RemovePage(int pageIndex)
    {
        if (pageIndex < 0 || pageIndex >= Pages.Count)
        {
            _logger.Warning("Attempted to remove page at invalid index: {Index}", pageIndex);
            return false;
        }

        // Don't allow removing the last page
        if (Pages.Count == 1)
        {
            _logger.Warning("Cannot remove the last page from document");
            return false;
        }

        Pages.RemoveAt(pageIndex);
        
        // Renumber remaining pages
        for (int i = pageIndex; i < Pages.Count; i++)
        {
            Pages[i].PageNumber = i + 1;
        }

        // Adjust current page index if necessary
        if (CurrentPageIndex >= Pages.Count)
        {
            CurrentPageIndex = Pages.Count - 1;
        }

        IsDirty = true;
        OnPropertyChanged(nameof(PageCount));
        
        _logger.Information("Page at index {Index} removed. Total pages: {PageCount}", 
            pageIndex, PageCount);
        
        return true;
    }

    /// <summary>
    /// Gets a page by index (0-based)
    /// </summary>
    public Page? GetPage(int pageIndex)
    {
        if (pageIndex < 0 || pageIndex >= Pages.Count)
        {
            _logger.Warning("Attempted to get page at invalid index: {Index}", pageIndex);
            return null;
        }

        return Pages[pageIndex];
    }

    /// <summary>
    /// Gets the current page
    /// </summary>
    public Page? GetCurrentPage()
    {
        return GetPage(CurrentPageIndex);
    }

    /// <summary>
    /// Navigates to the next page
    /// </summary>
    public bool GoToNextPage()
    {
        if (CurrentPageIndex < Pages.Count - 1)
        {
            CurrentPageIndex++;
            _logger.Debug("Navigated to page {PageNumber}", CurrentPageNumber);
            return true;
        }

        _logger.Debug("Already at last page");
        return false;
    }

    /// <summary>
    /// Navigates to the previous page
    /// </summary>
    public bool GoToPreviousPage()
    {
        if (CurrentPageIndex > 0)
        {
            CurrentPageIndex--;
            _logger.Debug("Navigated to page {PageNumber}", CurrentPageNumber);
            return true;
        }

        _logger.Debug("Already at first page");
        return false;
    }

    /// <summary>
    /// Navigates to a specific page by index (0-based)
    /// </summary>
    public bool GoToPage(int pageIndex)
    {
        if (pageIndex < 0 || pageIndex >= Pages.Count)
        {
            _logger.Warning("Attempted to navigate to invalid page index: {Index}", pageIndex);
            return false;
        }

        CurrentPageIndex = pageIndex;
        _logger.Debug("Navigated to page {PageNumber}", CurrentPageNumber);
        return true;
    }

    /// <summary>
    /// Navigates to the first page
    /// </summary>
    public void GoToFirstPage()
    {
        CurrentPageIndex = 0;
        _logger.Debug("Navigated to first page");
    }

    /// <summary>
    /// Navigates to the last page
    /// </summary>
    public void GoToLastPage()
    {
        CurrentPageIndex = Pages.Count - 1;
        _logger.Debug("Navigated to last page");
    }

    /// <summary>
    /// Clears all pages and adds a new empty page
    /// </summary>
    public void ClearPages()
    {
        Pages.Clear();
        AddPage();
        CurrentPageIndex = 0;
        IsDirty = true;
        
        _logger.Information("All pages cleared, new empty page added");
    }

    /// <summary>
    /// Marks the document as saved (not dirty)
    /// </summary>
    public void MarkAsSaved()
    {
        IsDirty = false;
        _logger.Information("Document marked as saved");
    }

    #endregion
}

/// <summary>
/// Event arguments for margin changes
/// </summary>
public class MarginsChangedEventArgs : EventArgs
{
    public DocumentMargins OldMargins { get; }
    public DocumentMargins NewMargins { get; }

    public MarginsChangedEventArgs(DocumentMargins oldMargins, DocumentMargins newMargins)
    {
        OldMargins = oldMargins;
        NewMargins = newMargins;
    }
}
