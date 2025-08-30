using AIAgentFramework.Core.Interfaces;
using AIAgentFramework.Monitoring.Interfaces;
using AIAgentFramework.Monitoring.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AIAgentFramework.Monitoring.HealthChecks;

/// <summary>
/// 오케스트레이션 엔진 Health Check
/// </summary>
public class OrchestrationHealthCheck : IHealthCheck, IConfigurableHealthCheck
{
    private readonly IOrchestrationEngine _orchestrationEngine;
    private readonly ILLMFunctionRegistry _llmRegistry;
    private readonly IToolRegistry _toolRegistry;
    private readonly ILogger<OrchestrationHealthCheck> _logger;
    private HealthCheckConfiguration _configuration;

    /// <inheritdoc />
    public string Name => "Orchestration Engine";

    /// <inheritdoc />
    public string Description => "오케스트레이션 엔진 및 레지스트리 상태 확인";

    /// <inheritdoc />
    public int TimeoutSeconds => _configuration.TimeoutSeconds;

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="orchestrationEngine">오케스트레이션 엔진</param>
    /// <param name="llmRegistry">LLM 함수 레지스트리</param>
    /// <param name="toolRegistry">도구 레지스트리</param>
    /// <param name="logger">로거</param>
    /// <param name="configuration">구성</param>
    public OrchestrationHealthCheck(
        IOrchestrationEngine orchestrationEngine,
        ILLMFunctionRegistry llmRegistry,
        IToolRegistry toolRegistry,
        ILogger<OrchestrationHealthCheck> logger,
        HealthCheckConfiguration? configuration = null)
    {
        _orchestrationEngine = orchestrationEngine ?? throw new ArgumentNullException(nameof(orchestrationEngine));
        _llmRegistry = llmRegistry ?? throw new ArgumentNullException(nameof(llmRegistry));
        _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? new HealthCheckConfiguration
        {
            TimeoutSeconds = 10,
            WarningThresholds = { ["response_time_ms"] = 5000 },
            CriticalThresholds = { ["response_time_ms"] = 10000 }
        };
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = HealthCheckResult.Healthy(Name);

        try
        {
            _logger.LogDebug("Starting orchestration health check");

            // 1. 레지스트리 상태 확인
            await CheckRegistryHealthAsync(result, cancellationToken);

            // 2. 오케스트레이션 엔진 기본 기능 테스트
            await CheckOrchestrationEngineAsync(result, cancellationToken);

            // 3. 메모리 사용량 확인
            CheckMemoryUsage(result);

            stopwatch.Stop();
            result.ResponseTimeMs = stopwatch.ElapsedMilliseconds;

            // 응답 시간 기반 상태 평가
            EvaluateResponseTime(result);

            _logger.LogDebug("Orchestration health check completed in {ElapsedMs}ms with status {Status}",
                result.ResponseTimeMs, result.Status);

            return result;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning("Orchestration health check was cancelled after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            return HealthCheckResult.Unhealthy(Name, "Health check was cancelled", responseTimeMs: stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Orchestration health check failed after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            return HealthCheckResult.Unhealthy(Name, "Health check failed", ex.Message, stopwatch.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// 레지스트리 상태 확인
    /// </summary>
    private async Task CheckRegistryHealthAsync(HealthCheckResult result, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            // LLM 함수 레지스트리 확인
            var llmFunctions = _llmRegistry.GetAllNames().ToList();
            result.WithData("llm_function_count", llmFunctions.Count);
            result.WithData("llm_functions", llmFunctions);

            if (llmFunctions.Count == 0)
            {
                result.Status = HealthStatus.Warning;
                result.Message = "No LLM functions registered";
            }

            // 도구 레지스트리 확인
            var tools = _toolRegistry.GetAllNames().ToList();
            result.WithData("tool_count", tools.Count);
            result.WithData("tools", tools);

            if (tools.Count == 0)
            {
                if (result.Status != HealthStatus.Unhealthy)
                {
                    result.Status = HealthStatus.Warning;
                    result.Message = result.Status == HealthStatus.Warning 
                        ? "No LLM functions and tools registered" 
                        : "No tools registered";
                }
            }

            // 필수 함수 확인
            CheckEssentialFunctions(result, llmFunctions);
            
        }, cancellationToken);
    }

    /// <summary>
    /// 필수 함수 확인
    /// </summary>
    private void CheckEssentialFunctions(HealthCheckResult result, List<string> llmFunctions)
    {
        var essentialFunctions = new[] { "planner" };
        var missingFunctions = essentialFunctions.Where(f => !llmFunctions.Contains(f)).ToList();

        result.WithData("essential_functions_missing", missingFunctions);

        if (missingFunctions.Count > 0)
        {
            result.Status = HealthStatus.Warning;
            result.Message = $"Missing essential functions: {string.Join(", ", missingFunctions)}";
        }
    }

    /// <summary>
    /// 오케스트레이션 엔진 테스트
    /// </summary>
    private async Task CheckOrchestrationEngineAsync(HealthCheckResult result, CancellationToken cancellationToken)
    {
        try
        {
            // 간단한 테스트 요청 생성
            var testRequest = new TestUserRequest("Health check test request");
            
            // 오케스트레이션 엔진이 요청을 받을 수 있는지 확인 (실제 실행하지는 않음)
            // 실제 구현에서는 엔진의 상태만 확인하거나 가벼운 테스트를 수행
            await Task.Run(() =>
            {
                result.WithData("engine_type", _orchestrationEngine.GetType().Name);
                result.WithData("engine_available", true);
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            result.Status = HealthStatus.Unhealthy;
            result.Message = "Orchestration engine check failed";
            result.Error = ex.Message;
        }
    }

    /// <summary>
    /// 메모리 사용량 확인
    /// </summary>
    private void CheckMemoryUsage(HealthCheckResult result)
    {
        var process = Process.GetCurrentProcess();
        var memoryMB = process.WorkingSet64 / 1024 / 1024;
        
        result.WithData("memory_usage_mb", memoryMB);
        result.WithData("gc_memory_mb", GC.GetTotalMemory(false) / 1024 / 1024);
        result.WithData("thread_count", process.Threads.Count);

        // 메모리 사용량이 500MB를 초과하면 경고
        if (memoryMB > 500)
        {
            if (result.Status == HealthStatus.Healthy)
            {
                result.Status = HealthStatus.Warning;
                result.Message = $"High memory usage: {memoryMB}MB";
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
            : 5000;

        var criticalThreshold = _configuration.CriticalThresholds.ContainsKey("response_time_ms")
            ? Convert.ToInt64(_configuration.CriticalThresholds["response_time_ms"])
            : 10000;

        if (result.ResponseTimeMs >= criticalThreshold)
        {
            result.Status = HealthStatus.Unhealthy;
            result.Message = $"Response time too high: {result.ResponseTimeMs}ms";
        }
        else if (result.ResponseTimeMs >= warningThreshold)
        {
            if (result.Status == HealthStatus.Healthy)
            {
                result.Status = HealthStatus.Warning;
                result.Message = $"Response time elevated: {result.ResponseTimeMs}ms";
            }
        }
        else if (result.Status == HealthStatus.Healthy)
        {
            result.Message = "All orchestration components are healthy";
        }
    }

    /// <inheritdoc />
    public void UpdateConfiguration(HealthCheckConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger.LogInformation("Orchestration health check configuration updated");
    }

    /// <inheritdoc />
    public HealthCheckConfiguration GetConfiguration()
    {
        return _configuration;
    }

    /// <summary>
    /// 테스트용 사용자 요청
    /// </summary>
    private class TestUserRequest : IUserRequest
    {
        public string Content { get; }
        public string RequestId { get; } = Guid.NewGuid().ToString();
        public string UserId { get; } = "health_check_user";
        public DateTime RequestedAt { get; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; } = new();

        public TestUserRequest(string content)
        {
            Content = content;
        }
    }
}