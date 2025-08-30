using Microsoft.Extensions.Logging;

namespace AIAgentFramework.ErrorHandling;

public class FaultTolerantExecutor
{
    private readonly ILogger<FaultTolerantExecutor> _logger;

    public FaultTolerantExecutor(ILogger<FaultTolerantExecutor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, int maxRetries = 3, TimeSpan? delay = null)
    {
        var actualDelay = delay ?? TimeSpan.FromSeconds(1);
        Exception? lastException = null;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogWarning(ex, "작업 실패 (시도 {Attempt}/{MaxRetries})", attempt, maxRetries);

                if (attempt < maxRetries)
                {
                    await Task.Delay(actualDelay);
                    actualDelay = TimeSpan.FromMilliseconds(actualDelay.TotalMilliseconds * 2); // 지수 백오프
                }
            }
        }

        throw lastException ?? new InvalidOperationException("알 수 없는 오류");
    }

    public async Task<T> ExecuteWithFallbackAsync<T>(Func<Task<T>> primaryOperation, Func<Task<T>> fallbackOperation)
    {
        try
        {
            return await primaryOperation();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "주 작업 실패, 대체 작업 실행");
            return await fallbackOperation();
        }
    }

    public async Task<T> ExecuteWithTimeoutAsync<T>(Func<Task<T>> operation, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        
        try
        {
            return await operation();
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
        {
            throw new TimeoutException($"작업이 {timeout.TotalSeconds}초 내에 완료되지 않았습니다.");
        }
    }
}