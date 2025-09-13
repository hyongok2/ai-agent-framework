using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AIAgentFramework.Core.Infrastructure.Resilience;
/// <summary>
/// 여러 복원력 정책을 조합한 파이프라인
/// </summary>
public class ResiliencePipeline : IResiliencePolicy
{
    private readonly List<IResiliencePolicy> _policies;
    private readonly ILogger<ResiliencePipeline>? _logger;

    public ResiliencePipeline(ILogger<ResiliencePipeline>? logger = null)
    {
        _policies = new List<IResiliencePolicy>();
        _logger = logger;
    }

    /// <summary>
    /// 정책 추가 (순서대로 적용됨)
    /// </summary>
    public ResiliencePipeline AddPolicy(IResiliencePolicy policy)
    {
        _policies.Add(policy);
        _logger?.LogDebug("복원력 정책 추가: {PolicyType}", policy.GetType().Name);
        return this;
    }

    /// <summary>
    /// 재시도 정책 추가
    /// </summary>
    public ResiliencePipeline AddRetry(
        int maxRetries = 3,
        int initialDelayMs = 1000,
        ILogger<RetryPolicy>? logger = null)
    {
        return AddPolicy(new RetryPolicy(maxRetries, initialDelayMs, logger));
    }

    /// <summary>
    /// Circuit Breaker 추가
    /// </summary>
    public ResiliencePipeline AddCircuitBreaker(
        int failureThreshold = 5,
        int openDurationSeconds = 60,
        ILogger<CircuitBreaker>? logger = null)
    {
        return AddPolicy(new CircuitBreaker(failureThreshold, openDurationSeconds, logger));
    }

    /// <summary>
    /// 타임아웃 정책 추가
    /// </summary>
    public ResiliencePipeline AddTimeout(TimeSpan timeout)
    {
        return AddPolicy(new TimeoutPolicy(timeout, _logger));
    }

    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        if (_policies.Count == 0)
        {
            // 정책이 없으면 그냥 실행
            return await operation(cancellationToken);
        }

        // 정책을 역순으로 중첩 (먼저 추가된 정책이 바깥쪽)
        Func<CancellationToken, Task<T>> wrappedOperation = operation;
        
        for (int i = _policies.Count - 1; i >= 0; i--)
        {
            var policy = _policies[i];
            var currentOperation = wrappedOperation;
            wrappedOperation = ct => policy.ExecuteAsync(currentOperation, ct);
        }

        return await wrappedOperation(cancellationToken);
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
}

/// <summary>
/// 타임아웃 정책
/// </summary>
public class TimeoutPolicy : IResiliencePolicy
{
    private readonly TimeSpan _timeout;
    private readonly ILogger? _logger;

    public TimeoutPolicy(TimeSpan timeout, ILogger? logger = null)
    {
        _timeout = timeout;
        _logger = logger;
    }

    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_timeout);

        try
        {
            _logger?.LogDebug("작업 시작 (타임아웃: {Timeout}ms)", _timeout.TotalMilliseconds);
            return await operation(cts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger?.LogWarning("작업 타임아웃: {Timeout}ms 초과", _timeout.TotalMilliseconds);
            throw new TimeoutException($"작업이 {_timeout.TotalSeconds}초 내에 완료되지 않았습니다");
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
}

/// <summary>
/// Fallback 정책
/// </summary>
public class FallbackPolicy<T> : IResiliencePolicy
{
    private readonly Func<Exception, CancellationToken, Task<T>> _fallbackOperation;
    private readonly ILogger? _logger;

    public FallbackPolicy(
        Func<Exception, CancellationToken, Task<T>> fallbackOperation,
        ILogger? logger = null)
    {
        _fallbackOperation = fallbackOperation;
        _logger = logger;
    }

    public async Task<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await operation(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "기본 작업 실패, Fallback 실행");
            
            if (typeof(TResult) == typeof(T))
            {
                var fallbackResult = await _fallbackOperation(ex, cancellationToken);
                return (TResult)(object)fallbackResult!;
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
}