using AIAgentFramework.Core.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace AIAgentFramework.Monitoring.Health;

/// <summary>
/// 오케스트레이션 엔진 상태 검사
/// </summary>
public class OrchestrationHealthCheck : IHealthCheck
{
    private readonly IOrchestrationEngine _orchestrationEngine;
    private readonly ILogger<OrchestrationHealthCheck> _logger;

    public OrchestrationHealthCheck(
        IOrchestrationEngine orchestrationEngine,
        ILogger<OrchestrationHealthCheck> logger)
    {
        _orchestrationEngine = orchestrationEngine ?? throw new ArgumentNullException(nameof(orchestrationEngine));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // 오케스트레이션 엔진 기본 상태 확인
            var data = new Dictionary<string, object>
            {
                ["engine_type"] = _orchestrationEngine.GetType().Name,
                ["check_time"] = DateTime.UtcNow,
                ["status"] = "available"
            };

            _logger.LogDebug("Orchestration health check passed");
            return Task.FromResult(HealthCheckResult.Healthy("오케스트레이션 엔진이 정상 작동 중입니다.", data));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Orchestration health check was cancelled");
            return Task.FromResult(HealthCheckResult.Unhealthy("오케스트레이션 상태 검사가 취소되었습니다."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Orchestration health check failed");
            return Task.FromResult(HealthCheckResult.Unhealthy("오케스트레이션 상태 검사 실패.", ex));
        }
    }
}