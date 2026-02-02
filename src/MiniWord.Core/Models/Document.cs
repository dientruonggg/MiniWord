namespace MiniWord.Core.Models;

/// <summary>
/// Represents the root container for the entire document.
/// A document contains a collection of paragraphs.
/// </summary>
public class Document
{
    /// <summary>
    /// Collection of paragraphs that make up the document.
    /// </summary>
    public List<Paragraph> Paragraphs { get; set; } = new();

    /// <summary>
    /// Document title/filename.
    /// </summary>
    public string Title { get; set; } = "Untitled";

    /// <summary>
    /// Indicates if the document has been modified since last save.
    /// </summary>
    public bool IsModified { get; set; } = false;

    /// <summary>
    /// Date and time when the document was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Date and time when the document was last modified.
    /// </summary>
    public DateTime LastModifiedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Gets the total number of paragraphs in the document.
    /// </summary>
    public int ParagraphCount => Paragraphs.Count;

    /// <summary>
    /// Gets the total character count across all paragraphs.
    /// </summary>
    public int CharacterCount => Paragraphs.Sum(p => p.GetText().Length);

    /// <summary>
    /// Gets the total word count across all paragraphs.
    /// </summary>
    public int WordCount
    {
        get
        {
            return Paragraphs.Sum(p =>
            {
                var text = p.GetText();
                if (string.IsNullOrWhiteSpace(text))
                    return 0;
                return text.Split(new[] { ' ', '\t', '\n', '\r' }, 
                    StringSplitOptions.RemoveEmptyEntries).Length;
            });
        }
    }

    /// <summary>
    /// Adds a new paragraph to the document.
    /// </summary>
    public void AddParagraph(Paragraph paragraph)
    {
        Paragraphs.Add(paragraph);
        MarkAsModified();
    }

    /// <summary>
    /// Inserts a paragraph at the specified index.
    /// </summary>
    public void InsertParagraph(int index, Paragraph paragraph)
    {
        if (index < 0 || index > Paragraphs.Count)
            throw new ArgumentOutOfRangeException(nameof(index), 
                "Index must be within the bounds of the paragraph collection.");
        
        Paragraphs.Insert(index, paragraph);
        MarkAsModified();
    }

    /// <summary>
    /// Removes a paragraph at the specified index.
    /// </summary>
    public void RemoveParagraph(int index)
    {
        if (index < 0 || index >= Paragraphs.Count)
            throw new ArgumentOutOfRangeException(nameof(index), 
                "Index must be within the bounds of the paragraph collection.");
        
        Paragraphs.RemoveAt(index);
        MarkAsModified();
    }

    /// <summary>
    /// Clears all paragraphs from the document.
    /// </summary>
    public void Clear()
    {
        Paragraphs.Clear();
        MarkAsModified();
    }

    /// <summary>
    /// Marks the document as modified and updates the last modified timestamp.
    /// </summary>
    public void MarkAsModified()
    {
        IsModified = true;
        LastModifiedAt = DateTime.Now;
    }

    /// <summary>
    /// Marks the document as saved (not modified).
    /// </summary>
    public void MarkAsSaved()
    {
        IsModified = false;
    }
}
