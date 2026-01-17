using Microsoft.Extensions.Logging;

namespace ConsolidatedService.Infrastructure.Resilience;

public class SimpleCircuitBreaker
{
    private readonly int _failureThreshold;
    private readonly TimeSpan _durationOfBreak;
    private readonly ILogger _logger;
    private int _failureCount;
    private DateTime? _lastFailureTime;
    private CircuitState _state = CircuitState.Closed;

    public SimpleCircuitBreaker(
        int failureThreshold,
        TimeSpan durationOfBreak,
        ILogger logger)
    {
        _failureThreshold = failureThreshold;
        _durationOfBreak = durationOfBreak;
        _logger = logger;
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
    {
        if (_state == CircuitState.Open)
        {
            if (DateTime.UtcNow - _lastFailureTime > _durationOfBreak)
            {
                _logger.LogInformation("Circuit breaker half-open. Testing connection.");
                _state = CircuitState.HalfOpen;
            }
            else
            {
                _logger.LogWarning("Circuit breaker is open. Operation not executed.");
                throw new InvalidOperationException("Circuit breaker is open");
            }
        }

        try
        {
            var result = await action();
            
            if (_state == CircuitState.HalfOpen)
            {
                _logger.LogInformation("Circuit breaker reset. Resuming normal operation.");
                _state = CircuitState.Closed;
                _failureCount = 0;
            }
            else
            {
                _failureCount = 0;
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _failureCount++;
            _lastFailureTime = DateTime.UtcNow;

            if (_failureCount >= _failureThreshold)
            {
                _state = CircuitState.Open;
                _logger.LogWarning(
                    "Circuit breaker opened after {FailureCount} failures. Duration: {Duration}s. Exception: {Exception}",
                    _failureCount,
                    _durationOfBreak.TotalSeconds,
                    ex.Message);
            }

            throw;
        }
    }

    public async Task ExecuteAsync(Func<Task> action)
    {
        await ExecuteAsync(async () =>
        {
            await action();
            return true;
        });
    }

    private enum CircuitState
    {
        Closed,
        Open,
        HalfOpen
    }
}
