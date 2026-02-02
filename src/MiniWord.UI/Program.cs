using Avalonia;
using System;
using Microsoft.Extensions.Configuration;
using Serilog;
using MiniWord.Core.Services;

namespace MiniWord.UI;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // Initialize Serilog logger early in the application lifecycle
        InitializeLogger();

        try
        {
            LoggerService.LogInfo("MiniWord application starting...");
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            LoggerService.LogException(ex, "Application startup");
            throw;
        }
        finally
        {
            LoggerService.LogInfo("MiniWord application shutting down...");
            LoggerService.CloseAndFlush();
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    /// <summary>
    /// Initializes Serilog logger from appsettings.json configuration.
    /// Falls back to default configuration if appsettings.json is not found.
    /// </summary>
    private static void InitializeLogger()
    {
        try
        {
            // Build configuration from appsettings.json
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .Build();

            // Create Serilog logger from configuration
            var logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            // Initialize the LoggerService with the configured logger
            LoggerService.Initialize(logger);

            // Set Serilog as the global logger
            Log.Logger = logger;
        }
        catch
        {
            // If configuration fails, LoggerService will use its default configuration
            // when first accessed
        }
    }
}
