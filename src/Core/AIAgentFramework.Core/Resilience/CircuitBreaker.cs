using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AIAgentFramework.Core.Resilience;

/// <summary>
/// Circuit Breaker 패턴 구현
/// </summary>
public class CircuitBreaker : IResiliencePolicy
{
    private readonly int _failureThreshold;
    private readonly TimeSpan _openDuration;
    private readonly ILogger<CircuitBreaker>? _logger;
    
    private CircuitState _state = CircuitState.Closed;
    private int _failureCount = 0;
    private DateTime _lastFailureTime = DateTime.MinValue;
    private DateTime _openedAt = DateTime.MinValue;
    private readonly object _lock = new();

    public CircuitBreaker(
        int failureThreshold = 5,
        int openDurationSeconds = 60,
        ILogger<CircuitBreaker>? logger = null)
    {
        _failureThreshold = failureThreshold;
        _openDuration = TimeSpan.FromSeconds(openDurationSeconds);
        _logger = logger;
    }

    public CircuitState State 
    { 
        get 
        {
            lock (_lock)
            {
                return _state;
            }
        }
    }

    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            UpdateState();
            
            if (_state == CircuitState.Open)
            {
                _logger?.LogWarning("Circuit breaker is OPEN, 요청 거부");
                throw new CircuitBreakerOpenException(
                    $"Circuit breaker is open. Will retry after {_openedAt.Add(_openDuration):yyyy-MM-dd HH:mm:ss}");
            }
        }

        try
        {
            var result = await operation(cancellationToken);
            
            lock (_lock)
            {
                OnSuccess();
            }
            
            return result;
        }
        catch (Exception ex)
        {
            lock (_lock)
            {
                OnFailure(ex);
            }
            throw;
        }
    }

    public async Task ExecuteAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(async ct =>
        {
            await operation(ct);
            return true;
        }, cancellationToken);
    }

    private void UpdateState()
    {
        if (_state == CircuitState.Open)
        {
            if (DateTime.UtcNow - _openedAt >= _openDuration)
            {
                _logger?.LogInformation("Circuit breaker 상태 변경: OPEN -> HALF_OPEN");
                _state = CircuitState.HalfOpen;
                _failureCount = 0;
            }
        }
    }

    private void OnSuccess()
    {
        if (_state == CircuitState.HalfOpen)
        {
            _logger?.LogInformation("Circuit breaker 상태 변경: HALF_OPEN -> CLOSED");
            _state = CircuitState.Closed;
            _failureCount = 0;
        }
        else if (_state == CircuitState.Closed)
        {
            // 성공 시 실패 카운트 리셋
            if (_failureCount > 0)
            {
                _logger?.LogDebug("성공으로 인한 실패 카운트 리셋: {Count} -> 0", _failureCount);
                _failureCount = 0;
            }
        }
    }

    private void OnFailure(Exception ex)
    {
        _lastFailureTime = DateTime.UtcNow;
        _failureCount++;
        
        _logger?.LogWarning(ex, "작업 실패 (실패 횟수: {Count}/{Threshold})", 
            _failureCount, _failureThreshold);

        if (_state == CircuitState.HalfOpen)
        {
            // Half-Open 상태에서 실패하면 즉시 Open
            _logger?.LogWarning("Circuit breaker 상태 변경: HALF_OPEN -> OPEN");
            _state = CircuitState.Open;
            _openedAt = DateTime.UtcNow;
        }
        else if (_state == CircuitState.Closed && _failureCount >= _failureThreshold)
        {
            _logger?.LogWarning("Circuit breaker 상태 변경: CLOSED -> OPEN (임계값 도달)");
            _state = CircuitState.Open;
            _openedAt = DateTime.UtcNow;
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _logger?.LogInformation("Circuit breaker 수동 리셋");
            _state = CircuitState.Closed;
            _failureCount = 0;
            _lastFailureTime = DateTime.MinValue;
            _openedAt = DateTime.MinValue;
        }
    }
}

/// <summary>
/// Circuit Breaker 상태
/// </summary>
public enum CircuitState
{
    /// <summary>
    /// 정상 작동 (요청 허용)
    /// </summary>
    Closed,
    
    /// <summary>
    /// 차단 상태 (요청 거부)
    /// </summary>
    Open,
    
    /// <summary>
    /// 테스트 상태 (제한적 요청 허용)
    /// </summary>
    HalfOpen
}

/// <summary>
/// Circuit Breaker가 Open 상태일 때 발생하는 예외
/// </summary>
public class CircuitBreakerOpenException : Exception
{
    public CircuitBreakerOpenException(string message) : base(message) { }
    public CircuitBreakerOpenException(string message, Exception innerException) 
        : base(message, innerException) { }
}