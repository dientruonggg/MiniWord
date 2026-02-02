using Xunit;
using MiniWord.Core.Services;
using Serilog;

namespace MiniWord.Tests.Services;

public class LoggerServiceTests
{
    [Fact]
    public void LoggerService_ShouldInitializeWithDefaultConfiguration()
    {
        // Act - Access the logger to trigger default initialization
        var logger = LoggerService.Logger;

        // Assert
        Assert.NotNull(logger);
    }

    [Fact]
    public void LoggerService_ShouldAcceptCustomLogger()
    {
        // Arrange
        var customLogger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        // Act
        LoggerService.Initialize(customLogger);

        // Assert
        Assert.NotNull(LoggerService.Logger);
    }

    [Fact]
    public void LogInfo_ShouldNotThrowException()
    {
        // Act & Assert
        var exception = Record.Exception(() => LoggerService.LogInfo("Test info message"));
        Assert.Null(exception);
    }

    [Fact]
    public void LogWarning_ShouldNotThrowException()
    {
        // Act & Assert
        var exception = Record.Exception(() => LoggerService.LogWarning("Test warning message"));
        Assert.Null(exception);
    }

    [Fact]
    public void LogError_ShouldNotThrowException()
    {
        // Act & Assert
        var exception = Record.Exception(() => LoggerService.LogError("Test error message"));
        Assert.Null(exception);
    }

    [Fact]
    public void LogException_ShouldNotThrowException()
    {
        // Arrange
        var testException = new InvalidOperationException("Test exception");

        // Act & Assert
        var exception = Record.Exception(() => 
            LoggerService.LogException(testException, "Test context"));
        Assert.Null(exception);
    }

    [Fact]
    public void LogException_WithoutContext_ShouldNotThrowException()
    {
        // Arrange
        var testException = new InvalidOperationException("Test exception");

        // Act & Assert
        var exception = Record.Exception(() => 
            LoggerService.LogException(testException));
        Assert.Null(exception);
    }

    [Fact]
    public void LogDebug_ShouldNotThrowException()
    {
        // Act & Assert
        var exception = Record.Exception(() => LoggerService.LogDebug("Test debug message"));
        Assert.Null(exception);
    }
}
