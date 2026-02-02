namespace MiniWord.Core.Logging;

/// <summary>
/// Provides logging functionality for the MiniWord application.
/// All exceptions and important events are logged to /logs/miniword-runtime.txt.
/// </summary>
public class Logger
{
    private static readonly object _lockObj = new object();
    private static string? _logFilePath;

    /// <summary>
    /// Gets or sets the log file path. Defaults to /logs/miniword-runtime.txt.
    /// </summary>
    public static string LogFilePath
    {
        get
        {
            if (_logFilePath == null)
            {
                // Default to /logs/miniword-runtime.txt as per requirements
                var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "logs");
                var fullLogDir = Path.GetFullPath(logDir);
                
                // Ensure the logs directory exists
                if (!Directory.Exists(fullLogDir))
                {
                    Directory.CreateDirectory(fullLogDir);
                }
                
                _logFilePath = Path.Combine(fullLogDir, "miniword-runtime.txt");
            }
            return _logFilePath;
        }
        set => _logFilePath = value;
    }

    /// <summary>
    /// Logs an exception with context information.
    /// </summary>
    public static void LogException(Exception ex, string context = "")
    {
        var message = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] EXCEPTION";
        if (!string.IsNullOrEmpty(context))
        {
            message += $" in {context}";
        }
        message += $"\n{ex.GetType().Name}: {ex.Message}\nStackTrace:\n{ex.StackTrace}\n";
        
        if (ex.InnerException != null)
        {
            message += $"Inner Exception: {ex.InnerException.Message}\n";
        }
        
        WriteLog(message);
    }

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    public static void LogInfo(string message)
    {
        var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] INFO: {message}\n";
        WriteLog(logMessage);
    }

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    public static void LogWarning(string message)
    {
        var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] WARNING: {message}\n";
        WriteLog(logMessage);
    }

    /// <summary>
    /// Logs an error message.
    /// </summary>
    public static void LogError(string message)
    {
        var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR: {message}\n";
        WriteLog(logMessage);
    }

    /// <summary>
    /// Writes a log message to the log file in a thread-safe manner.
    /// </summary>
    private static void WriteLog(string message)
    {
        try
        {
            lock (_lockObj)
            {
                File.AppendAllText(LogFilePath, message);
            }
        }
        catch
        {
            // Silently fail if we can't write to the log file
            // to avoid cascading exceptions
        }
    }

    /// <summary>
    /// Clears the log file.
    /// </summary>
    public static void ClearLog()
    {
        try
        {
            lock (_lockObj)
            {
                if (File.Exists(LogFilePath))
                {
                    File.WriteAllText(LogFilePath, string.Empty);
                }
            }
        }
        catch
        {
            // Silently fail
        }
    }
}
