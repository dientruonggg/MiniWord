using System;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace MiniWord.UI.Utilities;

/// <summary>
/// Utility class for debouncing operations to avoid excessive calls
/// Used for text change events to prevent excessive reflow calculations
/// </summary>
public class DebounceTimer : IDisposable
{
    private readonly ILogger _logger;
    private readonly int _delayMilliseconds;
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private bool _disposed = false;

    /// <summary>
    /// Creates a new debounce timer with the specified delay
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="delayMilliseconds">Delay in milliseconds before action is executed (default: 300ms)</param>
    public DebounceTimer(ILogger logger, int delayMilliseconds = 300)
    {
        _logger = logger;
        _delayMilliseconds = delayMilliseconds;
        
        _logger.Debug("DebounceTimer created with delay: {Delay}ms", _delayMilliseconds);
    }

    /// <summary>
    /// Debounces an action - if called multiple times within the delay period,
    /// only the last call will execute after the delay
    /// </summary>
    /// <param name="action">Action to execute after debounce delay</param>
    public async Task DebounceAsync(Action action)
    {
        await _semaphore.WaitAsync();
        try
        {
            // Cancel any pending operation
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();

            var token = _cancellationTokenSource.Token;
            
            _logger.Debug("Debounce scheduled, will execute in {Delay}ms", _delayMilliseconds);

            // Release semaphore before waiting
            _semaphore.Release();

            try
            {
                await Task.Delay(_delayMilliseconds, token);
                
                // If not cancelled, execute the action
                if (!token.IsCancellationRequested)
                {
                    _logger.Debug("Debounce delay completed, executing action");
                    action();
                }
                else
                {
                    _logger.Debug("Debounce cancelled");
                }
            }
            catch (TaskCanceledException)
            {
                // Expected when debounce is cancelled
                _logger.Debug("Debounce task cancelled");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error in debounce timer");
            _semaphore.Release();
            throw;
        }
    }

    /// <summary>
    /// Cancels any pending debounced action
    /// </summary>
    public void Cancel()
    {
        _cancellationTokenSource?.Cancel();
        _logger.Debug("Debounce timer cancelled");
    }

    /// <summary>
    /// Disposes the debounce timer and cancels any pending operations
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _semaphore.Dispose();
            _disposed = true;
            
            _logger.Debug("DebounceTimer disposed");
        }
    }
}
