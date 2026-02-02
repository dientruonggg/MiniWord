using MiniWord.Core.Exceptions;
using MiniWord.Core.Models;
using Serilog;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MiniWord.Core.Services;

/// <summary>
/// Handles serialization and deserialization of A4Document to/from JSON format
/// </summary>
public class DocumentSerializer
{
    private readonly ILogger _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public DocumentSerializer(ILogger logger)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <summary>
    /// Serializes an A4Document to a JSON file
    /// </summary>
    /// <param name="document">The document to serialize</param>
    /// <param name="filePath">The file path to save to</param>
    /// <exception cref="DocumentException">Thrown when serialization fails</exception>
    public async Task SerializeAsync(A4Document document, string filePath)
    {
        if (document == null)
        {
            var ex = new DocumentException("Cannot serialize null document");
            _logger.Error(ex, "Attempted to serialize null document");
            throw ex;
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            var ex = new DocumentException("File path cannot be null or empty");
            _logger.Error(ex, "Attempted to serialize to invalid file path");
            throw ex;
        }

        try
        {
            _logger.Information("Serializing document to {FilePath}", filePath);

            // Create DTO for serialization
            var dto = new DocumentDto
            {
                Content = document.Content,
                Margins = new MarginsDto
                {
                    Left = document.Margins.Left,
                    Right = document.Margins.Right,
                    Top = document.Margins.Top,
                    Bottom = document.Margins.Bottom
                },
                Pages = document.Pages.Select(p => new PageDto
                {
                    PageNumber = p.PageNumber,
                    Content = p.Content,
                    Lines = p.Lines.Select(l => new TextLineDto
                    {
                        Content = l.Content,
                        StartIndex = l.StartIndex,
                        Width = l.Width,
                        IsHardBreak = l.IsHardBreak
                    }).ToList()
                }).ToList(),
                CurrentPageIndex = document.CurrentPageIndex
            };

            // Ensure directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.Debug("Created directory: {Directory}", directory);
            }

            // Serialize to JSON
            var json = JsonSerializer.Serialize(dto, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json);

            _logger.Information("Document successfully serialized to {FilePath}. Pages: {PageCount}, Content length: {ContentLength}",
                filePath, document.PageCount, document.Content.Length);
        }
        catch (UnauthorizedAccessException ex)
        {
            var docEx = new DocumentException($"Access denied when writing to file: {filePath}", ex);
            _logger.Error(docEx, "Unauthorized access to file {FilePath}", filePath);
            throw docEx;
        }
        catch (IOException ex)
        {
            var docEx = new DocumentException($"I/O error when writing to file: {filePath}", ex);
            _logger.Error(docEx, "I/O error writing to file {FilePath}", filePath);
            throw docEx;
        }
        catch (Exception ex)
        {
            var docEx = new DocumentException($"Unexpected error serializing document to {filePath}", ex);
            _logger.Error(docEx, "Unexpected error during serialization");
            throw docEx;
        }
    }

    /// <summary>
    /// Deserializes an A4Document from a JSON file
    /// </summary>
    /// <param name="filePath">The file path to load from</param>
    /// <param name="logger">Logger instance for creating the document</param>
    /// <returns>The deserialized A4Document</returns>
    /// <exception cref="DocumentException">Thrown when deserialization fails</exception>
    public async Task<A4Document> DeserializeAsync(string filePath, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            var ex = new DocumentException("File path cannot be null or empty");
            _logger.Error(ex, "Attempted to deserialize from invalid file path");
            throw ex;
        }

        if (!File.Exists(filePath))
        {
            var ex = new DocumentException($"File not found: {filePath}");
            _logger.Error(ex, "File does not exist: {FilePath}", filePath);
            throw ex;
        }

        try
        {
            _logger.Information("Deserializing document from {FilePath}", filePath);

            // Read JSON from file
            var json = await File.ReadAllTextAsync(filePath);

            // Deserialize from JSON
            var dto = JsonSerializer.Deserialize<DocumentDto>(json, _jsonOptions);
            
            if (dto == null)
            {
                var ex = new DocumentException($"Failed to deserialize document from {filePath}");
                _logger.Error(ex, "Deserialization returned null for {FilePath}", filePath);
                throw ex;
            }

            // Create new document
            var document = new A4Document(logger)
            {
                Content = dto.Content ?? string.Empty
            };

            // Restore margins if present
            if (dto.Margins != null)
            {
                var margins = new DocumentMargins(
                    dto.Margins.Left,
                    dto.Margins.Right,
                    dto.Margins.Top,
                    dto.Margins.Bottom
                );
                document.UpdateMargins(margins);
            }

            // Restore pages if present
            if (dto.Pages != null && dto.Pages.Count > 0)
            {
                document.Pages.Clear(); // Clear all pages including the initial one
                
                foreach (var pageDto in dto.Pages)
                {
                    var page = new Page(pageDto.PageNumber)
                    {
                        Content = pageDto.Content ?? string.Empty
                    };

                    // Restore text lines if present
                    if (pageDto.Lines != null)
                    {
                        page.Lines = pageDto.Lines.Select(l => new TextLine
                        {
                            Content = l.Content ?? string.Empty,
                            StartIndex = l.StartIndex,
                            Width = l.Width,
                            IsHardBreak = l.IsHardBreak
                        }).ToList();
                    }
                    
                    document.Pages.Add(page);
                }

                // Restore current page index
                if (dto.CurrentPageIndex >= 0 && dto.CurrentPageIndex < document.PageCount)
                {
                    document.GoToPage(dto.CurrentPageIndex);
                }
            }

            // Mark as saved (not dirty) since we just loaded it
            document.MarkAsSaved();

            _logger.Information("Document successfully deserialized from {FilePath}. Pages: {PageCount}, Content length: {ContentLength}",
                filePath, document.PageCount, document.Content.Length);

            return document;
        }
        catch (JsonException ex)
        {
            var docEx = new DocumentException($"Invalid JSON format in file: {filePath}", ex);
            _logger.Error(docEx, "JSON parsing error in file {FilePath}", filePath);
            throw docEx;
        }
        catch (UnauthorizedAccessException ex)
        {
            var docEx = new DocumentException($"Access denied when reading file: {filePath}", ex);
            _logger.Error(docEx, "Unauthorized access to file {FilePath}", filePath);
            throw docEx;
        }
        catch (IOException ex)
        {
            var docEx = new DocumentException($"I/O error when reading file: {filePath}", ex);
            _logger.Error(docEx, "I/O error reading file {FilePath}", filePath);
            throw docEx;
        }
        catch (DocumentException)
        {
            // Re-throw document exceptions as-is
            throw;
        }
        catch (Exception ex)
        {
            var docEx = new DocumentException($"Unexpected error deserializing document from {filePath}", ex);
            _logger.Error(docEx, "Unexpected error during deserialization");
            throw docEx;
        }
    }

    #region DTOs for Serialization

    /// <summary>
    /// Data Transfer Object for A4Document serialization
    /// </summary>
    private class DocumentDto
    {
        public string Content { get; set; } = string.Empty;
        public MarginsDto? Margins { get; set; }
        public List<PageDto> Pages { get; set; } = new();
        public int CurrentPageIndex { get; set; }
    }

    /// <summary>
    /// Data Transfer Object for DocumentMargins serialization
    /// </summary>
    private class MarginsDto
    {
        public double Left { get; set; }
        public double Right { get; set; }
        public double Top { get; set; }
        public double Bottom { get; set; }
    }

    /// <summary>
    /// Data Transfer Object for Page serialization
    /// </summary>
    private class PageDto
    {
        public int PageNumber { get; set; }
        public string Content { get; set; } = string.Empty;
        public List<TextLineDto> Lines { get; set; } = new();
    }

    /// <summary>
    /// Data Transfer Object for TextLine serialization
    /// </summary>
    private class TextLineDto
    {
        public string Content { get; set; } = string.Empty;
        public int StartIndex { get; set; }
        public double Width { get; set; }
        public bool IsHardBreak { get; set; }
    }

    #endregion
}
