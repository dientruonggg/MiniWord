using MiniWord.Core.Exceptions;
using Serilog;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MiniWord.Core.Services;

/// <summary>
/// Manages the Most Recently Used (MRU) list of document files
/// Persists to ~/.config/miniword/recent-files.json on Linux
/// </summary>
public class RecentFilesManager
{
    private readonly ILogger _logger;
    private readonly string _configFilePath;
    private readonly int _maxRecentFiles;
    private readonly JsonSerializerOptions _jsonOptions;
    private List<string> _recentFiles;

    /// <summary>
    /// Gets the list of recent files in MRU order (most recent first)
    /// </summary>
    public IReadOnlyList<string> RecentFiles => _recentFiles.AsReadOnly();

    /// <summary>
    /// Creates a new RecentFilesManager instance
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="maxRecentFiles">Maximum number of recent files to track (default: 10)</param>
    public RecentFilesManager(ILogger logger, int maxRecentFiles = 10)
    {
        _logger = logger;
        _maxRecentFiles = maxRecentFiles;
        _recentFiles = new List<string>();
        
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        // Set config path to ~/.config/miniword/recent-files.json on Linux
        var configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config",
            "miniword"
        );
        
        _configFilePath = Path.Combine(configDir, "recent-files.json");
        
        _logger.Information("RecentFilesManager initialized. Config path: {ConfigPath}, Max files: {MaxFiles}",
            _configFilePath, _maxRecentFiles);
    }

    /// <summary>
    /// Adds a file to the recent files list (or moves it to the top if already present)
    /// </summary>
    /// <param name="filePath">Full path to the file</param>
    public void AddRecentFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            _logger.Warning("Attempted to add null or empty file path to recent files");
            return;
        }

        try
        {
            // Normalize the path
            filePath = Path.GetFullPath(filePath);
            
            // Remove if already exists (to move to top)
            _recentFiles.Remove(filePath);
            
            // Add to top of list
            _recentFiles.Insert(0, filePath);
            
            // Trim list to max size
            if (_recentFiles.Count > _maxRecentFiles)
            {
                _recentFiles = _recentFiles.Take(_maxRecentFiles).ToList();
            }
            
            _logger.Information("Added file to recent files: {FilePath}. Total recent files: {Count}",
                filePath, _recentFiles.Count);
            
            // Persist to disk
            Save();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to add file to recent files: {FilePath}", filePath);
        }
    }

    /// <summary>
    /// Removes a file from the recent files list
    /// </summary>
    /// <param name="filePath">Full path to the file</param>
    public void RemoveRecentFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            _logger.Warning("Attempted to remove null or empty file path from recent files");
            return;
        }

        try
        {
            // Normalize the path
            filePath = Path.GetFullPath(filePath);
            
            if (_recentFiles.Remove(filePath))
            {
                _logger.Information("Removed file from recent files: {FilePath}", filePath);
                Save();
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to remove file from recent files: {FilePath}", filePath);
        }
    }

    /// <summary>
    /// Clears all recent files
    /// </summary>
    public void ClearRecentFiles()
    {
        try
        {
            _recentFiles.Clear();
            _logger.Information("Cleared all recent files");
            Save();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to clear recent files");
        }
    }

    /// <summary>
    /// Loads recent files from disk
    /// </summary>
    public void Load()
    {
        try
        {
            if (!File.Exists(_configFilePath))
            {
                _logger.Information("Recent files config not found at {ConfigPath}. Starting with empty list.",
                    _configFilePath);
                _recentFiles = new List<string>();
                return;
            }

            _logger.Information("Loading recent files from {ConfigPath}", _configFilePath);
            
            var json = File.ReadAllText(_configFilePath);
            var dto = JsonSerializer.Deserialize<RecentFilesDto>(json, _jsonOptions);
            
            if (dto?.Files != null)
            {
                // Filter out files that no longer exist
                _recentFiles = dto.Files
                    .Where(File.Exists)
                    .Take(_maxRecentFiles)
                    .ToList();
                
                var removedCount = (dto.Files.Count - _recentFiles.Count);
                if (removedCount > 0)
                {
                    _logger.Information("Removed {Count} non-existent files from recent list", removedCount);
                }
                
                _logger.Information("Loaded {Count} recent files", _recentFiles.Count);
            }
            else
            {
                _logger.Warning("Recent files config is empty or invalid");
                _recentFiles = new List<string>();
            }
        }
        catch (JsonException ex)
        {
            _logger.Error(ex, "Failed to parse recent files JSON from {ConfigPath}", _configFilePath);
            _recentFiles = new List<string>();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load recent files from {ConfigPath}", _configFilePath);
            _recentFiles = new List<string>();
        }
    }

    /// <summary>
    /// Saves recent files to disk
    /// </summary>
    public void Save()
    {
        try
        {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(_configFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.Debug("Created config directory: {Directory}", directory);
            }

            var dto = new RecentFilesDto
            {
                Files = _recentFiles
            };

            var json = JsonSerializer.Serialize(dto, _jsonOptions);
            File.WriteAllText(_configFilePath, json);
            
            _logger.Information("Saved {Count} recent files to {ConfigPath}", _recentFiles.Count, _configFilePath);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.Error(ex, "Access denied when writing recent files to {ConfigPath}", _configFilePath);
        }
        catch (IOException ex)
        {
            _logger.Error(ex, "I/O error when writing recent files to {ConfigPath}", _configFilePath);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Unexpected error saving recent files to {ConfigPath}", _configFilePath);
        }
    }

    /// <summary>
    /// Validates and cleans the recent files list by removing non-existent files
    /// </summary>
    public void ValidateAndClean()
    {
        try
        {
            var originalCount = _recentFiles.Count;
            _recentFiles = _recentFiles.Where(File.Exists).ToList();
            
            var removedCount = originalCount - _recentFiles.Count;
            if (removedCount > 0)
            {
                _logger.Information("Removed {Count} non-existent files from recent list", removedCount);
                Save();
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to validate and clean recent files");
        }
    }

    #region DTOs for Serialization

    /// <summary>
    /// Data Transfer Object for recent files serialization
    /// </summary>
    private class RecentFilesDto
    {
        public List<string> Files { get; set; } = new();
    }

    #endregion
}
