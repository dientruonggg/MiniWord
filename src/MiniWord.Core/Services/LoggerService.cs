using Serilog;

namespace MiniWord.Core.Services;

/// <summary>
/// Provides Serilog-based logging functionality for the MiniWord application.
/// This service wraps Serilog and provides centralized logging capabilities
/// with Console and File sinks configured for Linux environment.
/// All exceptions and important events are logged to /logs/miniword-runtime.txt.
/// </summary>
public static class LoggerService
{
    private static ILogger? _logger;
    private static readonly object _lockObj = new object();

    /// <summary>
    /// Gets the Serilog logger instance. Initializes with default configuration if not already set.
    /// </summary>
    public static ILogger Logger
    {
        get
        {
            if (_logger == null)
            {
                lock (_lockObj)
                {
                    if (_logger == null)
                    {
                        // Default configuration if Initialize was not called
                        InitializeDefault();
                    }
                }
            }
            return _logger!; // After InitializeDefault, _logger will never be null
        }
    }

    /// <summary>
    /// Initializes the logger with the provided Serilog ILogger instance.
    /// This should be called early in the application startup.
    /// </summary>
    /// <param name="logger">The configured Serilog ILogger instance</param>
    public static void Initialize(ILogger logger)
    {
        lock (_lockObj)
        {
            _logger = logger;
        }
    }

    /// <summary>
    /// Initializes the logger with a default configuration.
    /// Used as a fallback if Initialize was not called explicitly.
    /// </summary>
    private static void InitializeDefault()
    {
        // Use the current directory or a relative logs folder
        // This works better across different execution contexts
        var logDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");

        // Ensure the logs directory exists
        if (!Directory.Exists(logDir))
        {
            Directory.CreateDirectory(logDir);
        }

        var logFilePath = Path.Combine(logDir, "miniword-runtime.txt");

        _logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                logFilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    /// <param name="message">The message to log</param>
    public static void LogInfo(string message)
    {
        Logger.Information(message);
    }

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="message">The warning message to log</param>
    public static void LogWarning(string message)
    {
        Logger.Warning(message);
    }

    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="message">The error message to log</param>
    public static void LogError(string message)
    {
        Logger.Error(message);
    }

    /// <summary>
    /// Logs an exception with context information.
    /// </summary>
    /// <param name="ex">The exception to log</param>
    /// <param name="context">Optional context information</param>
    public static void LogException(Exception ex, string context = "")
    {
        if (!string.IsNullOrEmpty(context))
        {
            Logger.Error(ex, "Exception in {Context}", context);
        }
        else
        {
            Logger.Error(ex, "Exception occurred");
        }
    }

    /// <summary>
    /// Logs a debug message.
    /// </summary>
    /// <param name="message">The debug message to log</param>
    public static void LogDebug(string message)
    {
        Logger.Debug(message);
    }

    /// <summary>
    /// Closes and flushes the logger.
    /// Should be called when the application is shutting down.
    /// </summary>
    public static void CloseAndFlush()
    {
        if (_logger != null)
        {
            // Dispose the logger instance to properly flush
            (_logger as IDisposable)?.Dispose();
        }
    }
}
