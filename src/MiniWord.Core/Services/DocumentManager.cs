using MiniWord.Core.Models;
using MiniWord.Core.Logging;

namespace MiniWord.Core.Services;

/// <summary>
/// Manages document operations including text insertion, paragraph management,
/// and text flow logic.
/// </summary>
public class DocumentManager
{
    private Document _document;

    /// <summary>
    /// Gets the current document being managed.
    /// </summary>
    public Document Document => _document;

    /// <summary>
    /// Initializes a new instance of DocumentManager with a new document.
    /// </summary>
    public DocumentManager()
    {
        _document = new Document();
        Logger.LogInfo("DocumentManager initialized with new document");
    }

    /// <summary>
    /// Initializes a new instance of DocumentManager with an existing document.
    /// </summary>
    public DocumentManager(Document document)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
        Logger.LogInfo("DocumentManager initialized with existing document");
    }

    /// <summary>
    /// Creates a new empty document, replacing the current one.
    /// </summary>
    public void CreateNewDocument(string title = "Untitled")
    {
        try
        {
            _document = new Document { Title = title };
            Logger.LogInfo($"New document created: {title}");
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, "CreateNewDocument");
            throw;
        }
    }

    /// <summary>
    /// Inserts plain text into the document at the specified paragraph index.
    /// If the paragraph doesn't exist, creates new paragraphs as needed.
    /// </summary>
    public void InsertText(int paragraphIndex, string text, TextRun? formatting = null)
    {
        try
        {
            if (string.IsNullOrEmpty(text))
                return;

            // Ensure paragraph exists
            while (_document.Paragraphs.Count <= paragraphIndex)
            {
                _document.AddParagraph(new Paragraph());
            }

            var paragraph = _document.Paragraphs[paragraphIndex];
            var textRun = formatting?.Clone() ?? new TextRun();
            textRun.Text = text;
            paragraph.AddRun(textRun);

            Logger.LogInfo($"Text inserted at paragraph {paragraphIndex}: {text.Length} characters");
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, "InsertText");
            throw;
        }
    }

    /// <summary>
    /// Appends a new paragraph with text to the document.
    /// </summary>
    public void AppendParagraph(string text = "", TextRun? formatting = null)
    {
        try
        {
            var paragraph = new Paragraph();
            
            if (!string.IsNullOrEmpty(text))
            {
                var textRun = formatting?.Clone() ?? new TextRun();
                textRun.Text = text;
                paragraph.AddRun(textRun);
            }

            _document.AddParagraph(paragraph);
            Logger.LogInfo($"Paragraph appended: {text.Length} characters");
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, "AppendParagraph");
            throw;
        }
    }

    /// <summary>
    /// Inserts a new paragraph at the specified index.
    /// </summary>
    public void InsertParagraphAt(int index, string text = "", TextRun? formatting = null)
    {
        try
        {
            var paragraph = new Paragraph();
            
            if (!string.IsNullOrEmpty(text))
            {
                var textRun = formatting?.Clone() ?? new TextRun();
                textRun.Text = text;
                paragraph.AddRun(textRun);
            }

            _document.InsertParagraph(index, paragraph);
            Logger.LogInfo($"Paragraph inserted at index {index}");
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, "InsertParagraphAt");
            throw;
        }
    }

    /// <summary>
    /// Removes a paragraph at the specified index.
    /// </summary>
    public void RemoveParagraphAt(int index)
    {
        try
        {
            _document.RemoveParagraph(index);
            Logger.LogInfo($"Paragraph removed at index {index}");
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, "RemoveParagraphAt");
            throw;
        }
    }

    /// <summary>
    /// Merges two consecutive paragraphs into one.
    /// </summary>
    public void MergeParagraphs(int firstParagraphIndex)
    {
        try
        {
            if (firstParagraphIndex < 0 || firstParagraphIndex >= _document.Paragraphs.Count - 1)
            {
                throw new ArgumentOutOfRangeException(nameof(firstParagraphIndex),
                    "Cannot merge: invalid paragraph index or no next paragraph.");
            }

            var firstPara = _document.Paragraphs[firstParagraphIndex];
            var secondPara = _document.Paragraphs[firstParagraphIndex + 1];

            // Copy all runs from second paragraph to first
            foreach (var run in secondPara.Runs)
            {
                firstPara.AddRun(run);
            }

            // Remove the second paragraph
            _document.RemoveParagraph(firstParagraphIndex + 1);
            
            Logger.LogInfo($"Paragraphs merged at index {firstParagraphIndex}");
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, "MergeParagraphs");
            throw;
        }
    }

    /// <summary>
    /// Splits a paragraph at the specified run index, creating two paragraphs.
    /// </summary>
    public void SplitParagraph(int paragraphIndex, int runIndex)
    {
        try
        {
            if (paragraphIndex < 0 || paragraphIndex >= _document.Paragraphs.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(paragraphIndex));
            }

            var paragraph = _document.Paragraphs[paragraphIndex];
            
            if (runIndex < 0 || runIndex > paragraph.Runs.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(runIndex));
            }

            // Create new paragraph with runs after the split point
            var newParagraph = new Paragraph
            {
                Alignment = paragraph.Alignment,
                LineSpacing = paragraph.LineSpacing,
                LeftIndent = paragraph.LeftIndent,
                RightIndent = paragraph.RightIndent,
                SpacingBefore = paragraph.SpacingBefore,
                SpacingAfter = paragraph.SpacingAfter
            };

            // Move runs from split point to new paragraph
            var runsToMove = paragraph.Runs.Skip(runIndex).ToList();
            foreach (var run in runsToMove)
            {
                newParagraph.AddRun(run);
            }

            // Remove moved runs from original paragraph
            paragraph.Runs.RemoveRange(runIndex, paragraph.Runs.Count - runIndex);

            // Insert new paragraph after the original
            _document.InsertParagraph(paragraphIndex + 1, newParagraph);
            
            Logger.LogInfo($"Paragraph split at index {paragraphIndex}, run {runIndex}");
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, "SplitParagraph");
            throw;
        }
    }

    /// <summary>
    /// Clears all content from the document.
    /// </summary>
    public void ClearDocument()
    {
        try
        {
            _document.Clear();
            Logger.LogInfo("Document cleared");
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, "ClearDocument");
            throw;
        }
    }

    /// <summary>
    /// Gets the total number of paragraphs in the document.
    /// </summary>
    public int GetParagraphCount() => _document.ParagraphCount;

    /// <summary>
    /// Gets the total character count in the document.
    /// </summary>
    public int GetCharacterCount() => _document.CharacterCount;

    /// <summary>
    /// Gets the total word count in the document.
    /// </summary>
    public int GetWordCount() => _document.WordCount;
}
