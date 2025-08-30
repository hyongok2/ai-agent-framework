using AIAgentFramework.Core.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace AIAgentFramework.Monitoring.Health;

/// <summary>
/// LLM Provider 상태 검사
/// </summary>
public class LLMHealthCheck : IHealthCheck
{
    private readonly ILLMProvider _llmProvider;
    private readonly ILogger<LLMHealthCheck> _logger;

    public LLMHealthCheck(
        ILLMProvider llmProvider,
        ILogger<LLMHealthCheck> logger)
    {
        _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // LLM Provider 가용성 확인
            var startTime = DateTime.UtcNow;
            
            // 타임아웃 설정 (10초)
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            
            var isAvailable = await _llmProvider.IsAvailableAsync();
            var responseTime = DateTime.UtcNow - startTime;

            var data = new Dictionary<string, object>
            {
                ["provider_name"] = _llmProvider.Name,
                ["default_model"] = _llmProvider.DefaultModel,
                ["supported_models"] = _llmProvider.SupportedModels.Count,
                ["response_time_ms"] = responseTime.TotalMilliseconds,
                ["is_available"] = isAvailable
            };

            if (isAvailable)
            {
                _logger.LogDebug("LLM Provider health check passed: {ProviderName}, {ResponseTime}ms", 
                    _llmProvider.Name, responseTime.TotalMilliseconds);
                    
                // 응답 시간에 따른 상태 판단
                if (responseTime.TotalMilliseconds > 5000) // 5초 이상
                {
                    return HealthCheckResult.Degraded($"LLM Provider '{_llmProvider.Name}'가 느리게 응답합니다.");
                }
                
                return HealthCheckResult.Healthy($"LLM Provider '{_llmProvider.Name}'가 정상 작동 중입니다.", data);
            }
            else
            {
                _logger.LogWarning("LLM Provider is not available: {ProviderName}", _llmProvider.Name);
                return HealthCheckResult.Unhealthy($"LLM Provider '{_llmProvider.Name}'를 사용할 수 없습니다.");
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("LLM health check was cancelled");
            return HealthCheckResult.Unhealthy("LLM 상태 검사가 취소되었습니다.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LLM health check failed for provider: {ProviderName}", _llmProvider.Name);
            return HealthCheckResult.Unhealthy($"LLM Provider '{_llmProvider.Name}' 상태 검사 실패.", ex);
        }
    }
}