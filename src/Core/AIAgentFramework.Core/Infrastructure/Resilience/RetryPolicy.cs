using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AIAgentFramework.Core.Infrastructure.Resilience;

/// <summary>
/// 재시도 정책 구현
/// </summary>
public class RetryPolicy : IResiliencePolicy
{
    private readonly int _maxRetries;
    private readonly TimeSpan _initialDelay;
    private readonly ILogger<RetryPolicy>? _logger;
    private readonly Func<Exception, bool> _shouldRetry;

    public RetryPolicy(
        int maxRetries = 3,
        int initialDelayMs = 1000,
        ILogger<RetryPolicy>? logger = null,
        Func<Exception, bool>? shouldRetry = null)
    {
        _maxRetries = maxRetries;
        _initialDelay = TimeSpan.FromMilliseconds(initialDelayMs);
        _logger = logger;
        _shouldRetry = shouldRetry ?? DefaultShouldRetry;
    }

    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        int attempt = 0;
        TimeSpan delay = _initialDelay;

        while (attempt < _maxRetries)
        {
            try
            {
                attempt++;
                _logger?.LogDebug("실행 시도 {Attempt}/{MaxRetries}", attempt, _maxRetries);
                
                return await operation(cancellationToken);
            }
            catch (Exception ex) when (attempt < _maxRetries && _shouldRetry(ex))
            {
                _logger?.LogWarning(ex, 
                    "작업 실패 (시도 {Attempt}/{MaxRetries}), {Delay}ms 후 재시도",
                    attempt, _maxRetries, delay.TotalMilliseconds);
                
                await Task.Delay(delay, cancellationToken);
                
                // Exponential backoff
                delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "작업 최종 실패 (시도 {Attempt}/{MaxRetries})", attempt, _maxRetries);
                throw;
            }
        }

        throw new InvalidOperationException($"최대 재시도 횟수({_maxRetries}) 초과");
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

    private static bool DefaultShouldRetry(Exception ex)
    {
        // 기본적으로 다음 예외들은 재시도
        return ex is HttpRequestException ||
               ex is TaskCanceledException ||
               ex is TimeoutException ||
               ex is OperationCanceledException ||
               (ex.InnerException != null && DefaultShouldRetry(ex.InnerException));
    }
}

/// <summary>
/// 복원력 정책 인터페이스
/// </summary>
public interface IResiliencePolicy
{
    Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);
        
    Task ExecuteAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default);
}