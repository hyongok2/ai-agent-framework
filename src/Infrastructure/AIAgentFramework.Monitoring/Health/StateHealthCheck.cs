using AIAgentFramework.State.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace AIAgentFramework.Monitoring.Health;

/// <summary>
/// State Provider 상태 검사
/// </summary>
public class StateHealthCheck : IHealthCheck
{
    private readonly IStateProvider _stateProvider;
    private readonly ILogger<StateHealthCheck> _logger;

    public StateHealthCheck(
        IStateProvider stateProvider,
        ILogger<StateHealthCheck> logger)
    {
        _stateProvider = stateProvider ?? throw new ArgumentNullException(nameof(stateProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var healthCheckKey = $"health_check_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}";
        var testData = new { timestamp = DateTime.UtcNow, status = "health_check" };
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            // 타임아웃 설정 (5초)
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            
            // 1. 쓰기 테스트
            await _stateProvider.SetAsync(healthCheckKey, testData, TimeSpan.FromMinutes(1), combinedCts.Token);
            
            // 2. 읽기 테스트
            var retrievedData = await _stateProvider.GetAsync<object>(healthCheckKey, combinedCts.Token);
            
            // 3. 존재 확인 테스트
            var exists = await _stateProvider.ExistsAsync(healthCheckKey, combinedCts.Token);
            
            // 4. 삭제 테스트 (정리)
            await _stateProvider.DeleteAsync(healthCheckKey, combinedCts.Token);
            
            var responseTime = DateTime.UtcNow - startTime;
            
            var data = new Dictionary<string, object>
            {
                ["provider_type"] = _stateProvider.GetType().Name,
                ["response_time_ms"] = responseTime.TotalMilliseconds,
                ["write_success"] = true,
                ["read_success"] = retrievedData != null,
                ["exists_success"] = exists,
                ["delete_success"] = true
            };

            // 기본 CRUD 작업 검증
            if (retrievedData == null)
            {
                _logger.LogWarning("State provider read operation failed during health check");
                return HealthCheckResult.Degraded("상태 저장소 읽기 작업이 실패했습니다.");
            }
            
            if (!exists)
            {
                _logger.LogWarning("State provider exists check failed during health check");
                return HealthCheckResult.Degraded("상태 저장소 존재 확인이 실패했습니다.");
            }

            // 응답 시간에 따른 상태 판단
            if (responseTime.TotalMilliseconds > 3000) // 3초 이상
            {
                _logger.LogWarning("State provider is responding slowly: {ResponseTime}ms", responseTime.TotalMilliseconds);
                return HealthCheckResult.Degraded("상태 저장소가 느리게 응답합니다.");
            }

            _logger.LogDebug("State provider health check passed: {ResponseTime}ms", responseTime.TotalMilliseconds);
            return HealthCheckResult.Healthy("상태 저장소가 정상 작동 중입니다.", data);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("State provider health check was cancelled");
            
            // 정리 시도 (취소되었지만 가능하면)
            try { await _stateProvider.DeleteAsync(healthCheckKey, CancellationToken.None); } catch { }
            
            return HealthCheckResult.Unhealthy("상태 저장소 상태 검사가 취소되었습니다.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "State provider health check failed");
            
            // 정리 시도
            try { await _stateProvider.DeleteAsync(healthCheckKey, CancellationToken.None); } catch { }
            
            return HealthCheckResult.Unhealthy("상태 저장소 상태 검사 실패.", ex);
        }
    }
}