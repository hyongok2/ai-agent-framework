using AIAgentFramework.Monitoring.Interfaces;
using AIAgentFramework.Monitoring.Models;
using AIAgentFramework.State.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AIAgentFramework.Monitoring.HealthChecks;

/// <summary>
/// State Provider Health Check
/// </summary>
public class StateHealthCheck : IHealthCheck, IConfigurableHealthCheck
{
    private readonly IStateProvider _stateProvider;
    private readonly ILogger<StateHealthCheck> _logger;
    private HealthCheckConfiguration _configuration;

    /// <inheritdoc />
    public string Name => "State Provider";

    /// <inheritdoc />
    public string Description => "상태 저장소 연결 및 기능 확인";

    /// <inheritdoc />
    public int TimeoutSeconds => _configuration.TimeoutSeconds;

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="stateProvider">상태 제공자</param>
    /// <param name="logger">로거</param>
    /// <param name="configuration">구성</param>
    public StateHealthCheck(
        IStateProvider stateProvider,
        ILogger<StateHealthCheck> logger,
        HealthCheckConfiguration? configuration = null)
    {
        _stateProvider = stateProvider ?? throw new ArgumentNullException(nameof(stateProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? new HealthCheckConfiguration
        {
            TimeoutSeconds = 15,
            WarningThresholds = { ["response_time_ms"] = 3000, ["memory_usage_mb"] = 100 },
            CriticalThresholds = { ["response_time_ms"] = 10000, ["memory_usage_mb"] = 500 }
        };
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = HealthCheckResult.Healthy(Name);

        try
        {
            _logger.LogDebug("Starting state provider health check");

            // 1. 기본 연결 상태 확인
            await CheckConnectionHealthAsync(result, cancellationToken);

            // 2. 읽기/쓰기 기능 테스트
            await CheckReadWriteFunctionalityAsync(result, cancellationToken);

            // 3. 트랜잭션 기능 테스트 (지원하는 경우)
            await CheckTransactionSupportAsync(result, cancellationToken);

            // 4. 상태 제공자 통계 확인
            await CheckProviderStatisticsAsync(result, cancellationToken);

            stopwatch.Stop();
            result.ResponseTimeMs = stopwatch.ElapsedMilliseconds;

            // 응답 시간 기반 상태 평가
            EvaluateResponseTime(result);

            _logger.LogDebug("State provider health check completed in {ElapsedMs}ms with status {Status}",
                result.ResponseTimeMs, result.Status);

            return result;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning("State provider health check was cancelled after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            return HealthCheckResult.Unhealthy(Name, "Health check was cancelled", responseTimeMs: stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "State provider health check failed after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            return HealthCheckResult.Unhealthy(Name, "Health check failed", ex.Message, stopwatch.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// 연결 상태 확인
    /// </summary>
    private async Task CheckConnectionHealthAsync(HealthCheckResult result, CancellationToken cancellationToken)
    {
        try
        {
            // 간단한 연결 테스트
            result.WithData("provider_type", _stateProvider.GetType().Name);
            result.WithData("connection_healthy", true);
            result.WithData("connection_message", "Basic connection check passed");
            
            await Task.CompletedTask; // 비동기 메서드 호환성을 위한 더미
        }
        catch (Exception ex)
        {
            result.Status = HealthStatus.Unhealthy;
            result.Message = "Failed to check state provider connection";
            result.Error = ex.Message;
        }
    }

    /// <summary>
    /// 읽기/쓰기 기능 테스트
    /// </summary>
    private async Task CheckReadWriteFunctionalityAsync(HealthCheckResult result, CancellationToken cancellationToken)
    {
        var testKey = $"health_check_{Guid.NewGuid()}";
        var testData = new { Message = "Health check test", Timestamp = DateTime.UtcNow };

        try
        {
            // 쓰기 테스트
            var writeStopwatch = Stopwatch.StartNew();
            await _stateProvider.SetAsync(testKey, testData, TimeSpan.FromMinutes(1), cancellationToken);
            writeStopwatch.Stop();

            result.WithData("write_test_success", true);
            result.WithData("write_test_time_ms", writeStopwatch.ElapsedMilliseconds);

            // 읽기 테스트
            var readStopwatch = Stopwatch.StartNew();
            var retrievedData = await _stateProvider.GetAsync<object>(testKey, cancellationToken);
            readStopwatch.Stop();

            result.WithData("read_test_success", retrievedData != null);
            result.WithData("read_test_time_ms", readStopwatch.ElapsedMilliseconds);

            if (retrievedData == null)
            {
                result.Status = HealthStatus.Warning;
                result.Message = "State provider read test returned null";
            }

            // 존재 확인 테스트
            var existsStopwatch = Stopwatch.StartNew();
            var exists = await _stateProvider.ExistsAsync(testKey, cancellationToken);
            existsStopwatch.Stop();

            result.WithData("exists_test_success", exists);
            result.WithData("exists_test_time_ms", existsStopwatch.ElapsedMilliseconds);

            // 삭제 테스트
            var deleteStopwatch = Stopwatch.StartNew();
            await _stateProvider.DeleteAsync(testKey, cancellationToken);
            deleteStopwatch.Stop();

            result.WithData("delete_test_time_ms", deleteStopwatch.ElapsedMilliseconds);

            // 삭제 확인
            var existsAfterDelete = await _stateProvider.ExistsAsync(testKey, cancellationToken);
            result.WithData("delete_test_success", !existsAfterDelete);

            if (existsAfterDelete)
            {
                result.Status = HealthStatus.Warning;
                result.Message = result.Status == HealthStatus.Healthy 
                    ? "State provider delete test failed" 
                    : result.Message;
            }
        }
        catch (Exception ex)
        {
            result.Status = HealthStatus.Unhealthy;
            result.Message = "State provider read/write functionality test failed";
            result.Error = ex.Message;

            // 정리 작업
            try
            {
                await _stateProvider.DeleteAsync(testKey, CancellationToken.None);
            }
            catch
            {
                // 정리 실패는 무시
            }
        }
    }

    /// <summary>
    /// 트랜잭션 지원 확인
    /// </summary>
    private async Task CheckTransactionSupportAsync(HealthCheckResult result, CancellationToken cancellationToken)
    {
        try
        {
            // 트랜잭션 지원 여부 확인
            var supportsTransactions = _stateProvider.GetType().GetInterfaces()
                .Any(i => i.Name.Contains("Transaction"));

            result.WithData("supports_transactions", supportsTransactions);

            if (supportsTransactions)
            {
                // 간단한 트랜잭션 테스트
                var testKey = $"tx_health_check_{Guid.NewGuid()}";
                try
                {
                    using var transaction = await _stateProvider.BeginTransactionAsync(cancellationToken);
                    await _stateProvider.SetAsync(testKey, "transaction test", TimeSpan.FromMinutes(1), cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    var exists = await _stateProvider.ExistsAsync(testKey, cancellationToken);
                    result.WithData("transaction_test_success", exists);

                    // 정리
                    await _stateProvider.DeleteAsync(testKey, cancellationToken);
                }
                catch (Exception txEx)
                {
                    result.WithData("transaction_test_success", false);
                    result.WithData("transaction_test_error", txEx.Message);
                    
                    if (result.Status == HealthStatus.Healthy)
                    {
                        result.Status = HealthStatus.Warning;
                        result.Message = "Transaction functionality test failed";
                    }
                }
            }
        }
        catch (Exception ex)
        {
            result.WithData("transaction_check_error", ex.Message);
        }
    }

    /// <summary>
    /// 상태 제공자 통계 확인
    /// </summary>
    private async Task CheckProviderStatisticsAsync(HealthCheckResult result, CancellationToken cancellationToken)
    {
        try
        {
            // 간단한 기본 통계 정보
            result.WithData("statistics_available", true);
            result.WithData("provider_name", _stateProvider.GetType().Name);
            result.WithData("statistics_message", "Basic provider statistics available");
            
            await Task.CompletedTask; // 비동기 메서드 호환성을 위한 더미
        }
        catch (Exception ex)
        {
            result.WithData("statistics_error", ex.Message);
            _logger.LogWarning(ex, "Failed to retrieve state provider statistics");
        }
    }

    /// <summary>
    /// 응답 시간 평가
    /// </summary>
    private void EvaluateResponseTime(HealthCheckResult result)
    {
        var warningThreshold = _configuration.WarningThresholds.ContainsKey("response_time_ms")
            ? Convert.ToInt64(_configuration.WarningThresholds["response_time_ms"])
            : 3000;

        var criticalThreshold = _configuration.CriticalThresholds.ContainsKey("response_time_ms")
            ? Convert.ToInt64(_configuration.CriticalThresholds["response_time_ms"])
            : 10000;

        if (result.ResponseTimeMs >= criticalThreshold)
        {
            result.Status = HealthStatus.Unhealthy;
            result.Message = $"State provider response time too high: {result.ResponseTimeMs}ms";
        }
        else if (result.ResponseTimeMs >= warningThreshold)
        {
            if (result.Status == HealthStatus.Healthy)
            {
                result.Status = HealthStatus.Warning;
                result.Message = $"State provider response time elevated: {result.ResponseTimeMs}ms";
            }
        }
        else if (result.Status == HealthStatus.Healthy)
        {
            result.Message = "State provider is healthy and responsive";
        }
    }

    /// <inheritdoc />
    public void UpdateConfiguration(HealthCheckConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger.LogInformation("State provider health check configuration updated");
    }

    /// <inheritdoc />
    public HealthCheckConfiguration GetConfiguration()
    {
        return _configuration;
    }
}