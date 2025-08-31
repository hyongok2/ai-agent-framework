
using AIAgentFramework.Core.LLM.Abstractions;

using AIAgentFramework.Monitoring.Interfaces;
using AIAgentFramework.Monitoring.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AIAgentFramework.Monitoring.HealthChecks;

/// <summary>
/// LLM Provider Health Check
/// </summary>
public class LLMHealthCheck : IHealthCheck, IConfigurableHealthCheck
{
    private readonly ILLMProvider _llmProvider;
    private readonly ILogger<LLMHealthCheck> _logger;
    private HealthCheckConfiguration _configuration;

    /// <inheritdoc />
    public string Name => "LLM Provider";

    /// <inheritdoc />
    public string Description => "LLM Provider 연결 및 응답 상태 확인";

    /// <inheritdoc />
    public int TimeoutSeconds => _configuration.TimeoutSeconds;

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="llmProvider">LLM Provider</param>
    /// <param name="logger">로거</param>
    /// <param name="configuration">구성</param>
    public LLMHealthCheck(
        ILLMProvider llmProvider,
        ILogger<LLMHealthCheck> logger,
        HealthCheckConfiguration? configuration = null)
    {
        _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? new HealthCheckConfiguration
        {
            TimeoutSeconds = 30,
            WarningThresholds = { ["response_time_ms"] = 10000, ["token_count"] = 1000 },
            CriticalThresholds = { ["response_time_ms"] = 30000, ["token_count"] = 2000 }
        };
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = HealthCheckResult.Healthy(Name);

        try
        {
            _logger.LogDebug("Starting LLM provider health check");

            // 1. Provider 가용성 확인
            await CheckProviderAvailabilityAsync(result, cancellationToken);

            // 2. 기본 생성 요청 테스트
            await CheckBasicGenerationAsync(result, cancellationToken);

            // 3. 토큰 카운팅 기능 테스트
            await CheckTokenCountingAsync(result, cancellationToken);

            stopwatch.Stop();
            result.ResponseTimeMs = stopwatch.ElapsedMilliseconds;

            // 응답 시간 기반 상태 평가
            EvaluateResponseTime(result);

            _logger.LogDebug("LLM provider health check completed in {ElapsedMs}ms with status {Status}",
                result.ResponseTimeMs, result.Status);

            return result;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning("LLM provider health check was cancelled after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            return HealthCheckResult.Unhealthy(Name, "Health check was cancelled", responseTimeMs: stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "LLM provider health check failed after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            return HealthCheckResult.Unhealthy(Name, "Health check failed", ex.Message, stopwatch.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Provider 가용성 확인
    /// </summary>
    private async Task CheckProviderAvailabilityAsync(HealthCheckResult result, CancellationToken cancellationToken)
    {
        try
        {
            var isAvailable = await _llmProvider.IsAvailableAsync();
            result.WithData("provider_available", isAvailable);
            result.WithData("provider_type", _llmProvider.GetType().Name);
            result.WithData("default_model", _llmProvider.DefaultModel);

            if (!isAvailable)
            {
                result.Status = HealthStatus.Unhealthy;
                result.Message = "LLM Provider is not available";
            }
        }
        catch (Exception ex)
        {
            result.Status = HealthStatus.Unhealthy;
            result.Message = "Failed to check provider availability";
            result.Error = ex.Message;
        }
    }

    /// <summary>
    /// 기본 생성 요청 테스트
    /// </summary>
    private async Task CheckBasicGenerationAsync(HealthCheckResult result, CancellationToken cancellationToken)
    {
        try
        {
            var testPrompt = "Hello, this is a health check test. Please respond with 'OK'.";
            
            var generationStopwatch = Stopwatch.StartNew();
            var response = await _llmProvider.GenerateAsync(testPrompt, cancellationToken);
            generationStopwatch.Stop();

            result.WithData("test_generation_success", !string.IsNullOrEmpty(response));
            result.WithData("test_generation_time_ms", generationStopwatch.ElapsedMilliseconds);
            result.WithData("test_response_length", response?.Length ?? 0);

            // 간단한 토큰 사용량 추정
            result.WithData("test_tokens_estimated", response?.Length / 4 ?? 0); // 대략적인 토큰 추정

            if (string.IsNullOrEmpty(response))
            {
                result.Status = HealthStatus.Warning;
                result.Message = "LLM generation returned empty response";
            }
            else
            {
                // 응답 시간 평가
                var warningThreshold = _configuration.WarningThresholds.ContainsKey("response_time_ms")
                    ? Convert.ToInt64(_configuration.WarningThresholds["response_time_ms"])
                    : 10000;

                if (generationStopwatch.ElapsedMilliseconds >= warningThreshold && result.Status == HealthStatus.Healthy)
                {
                    result.Status = HealthStatus.Warning;
                    result.Message = $"LLM generation took longer than expected: {generationStopwatch.ElapsedMilliseconds}ms";
                }
            }
        }
        catch (Exception ex)
        {
            result.Status = HealthStatus.Unhealthy;
            result.Message = "LLM generation test failed with exception";
            result.Error = ex.Message;
        }
    }

    /// <summary>
    /// 토큰 카운팅 기능 테스트
    /// </summary>
    private async Task CheckTokenCountingAsync(HealthCheckResult result, CancellationToken cancellationToken)
    {
        try
        {
            var testText = "This is a token counting test for health check purposes.";
            var tokenCount = await _llmProvider.CountTokensAsync(testText);
            
            result.WithData("token_counting_available", true);
            result.WithData("test_token_count", tokenCount);

            if (tokenCount <= 0)
            {
                result.Status = HealthStatus.Warning;
                result.Message = result.Status == HealthStatus.Healthy 
                    ? "Token counting returned zero or negative count" 
                    : result.Message;
            }
        }
        catch (NotSupportedException)
        {
            result.WithData("token_counting_available", false);
            result.WithData("token_counting_error", "Token counting not supported");
            
            if (result.Status == HealthStatus.Healthy)
            {
                result.Status = HealthStatus.Warning;
                result.Message = "Token counting is not supported by this provider";
            }
        }
        catch (Exception ex)
        {
            result.WithData("token_counting_available", false);
            result.WithData("token_counting_error", ex.Message);
            
            if (result.Status == HealthStatus.Healthy)
            {
                result.Status = HealthStatus.Warning;
                result.Message = "Token counting test failed";
            }
        }
    }

    /// <summary>
    /// 응답 시간 평가
    /// </summary>
    private void EvaluateResponseTime(HealthCheckResult result)
    {
        var warningThreshold = _configuration.WarningThresholds.ContainsKey("response_time_ms")
            ? Convert.ToInt64(_configuration.WarningThresholds["response_time_ms"])
            : 10000;

        var criticalThreshold = _configuration.CriticalThresholds.ContainsKey("response_time_ms")
            ? Convert.ToInt64(_configuration.CriticalThresholds["response_time_ms"])
            : 30000;

        if (result.ResponseTimeMs >= criticalThreshold)
        {
            result.Status = HealthStatus.Unhealthy;
            result.Message = $"LLM provider response time too high: {result.ResponseTimeMs}ms";
        }
        else if (result.ResponseTimeMs >= warningThreshold)
        {
            if (result.Status == HealthStatus.Healthy)
            {
                result.Status = HealthStatus.Warning;
                result.Message = $"LLM provider response time elevated: {result.ResponseTimeMs}ms";
            }
        }
        else if (result.Status == HealthStatus.Healthy)
        {
            result.Message = "LLM provider is healthy and responsive";
        }
    }

    /// <inheritdoc />
    public void UpdateConfiguration(HealthCheckConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger.LogInformation("LLM provider health check configuration updated");
    }

    /// <inheritdoc />
    public HealthCheckConfiguration GetConfiguration()
    {
        return _configuration;
    }
}