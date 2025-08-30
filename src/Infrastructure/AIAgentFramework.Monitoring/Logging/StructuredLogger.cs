using AIAgentFramework.Monitoring.Telemetry;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace AIAgentFramework.Monitoring.Logging;

/// <summary>
/// 구조화된 로깅을 위한 로거 래퍼
/// </summary>
public class StructuredLogger : IDisposable
{
    private readonly ILogger _logger;
    private readonly ActivitySourceManager _activitySourceManager;
    private readonly JsonSerializerOptions _jsonOptions;

    public StructuredLogger(ILogger logger, ActivitySourceManager activitySourceManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _activitySourceManager = activitySourceManager ?? throw new ArgumentNullException(nameof(activitySourceManager));
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <summary>
    /// 오케스트레이션 시작 로그
    /// </summary>
    public void LogOrchestrationStart(string sessionId, string requestType, Dictionary<string, object>? metadata = null)
    {
        using var activity = _activitySourceManager.StartOrchestrationActivity(sessionId, "orchestration_start");
        
        var logData = new
        {
            EventType = "orchestration_start",
            SessionId = sessionId,
            RequestType = requestType,
            Timestamp = DateTimeOffset.UtcNow,
            Metadata = metadata ?? new Dictionary<string, object>()
        };

        if (activity != null)
        {
            activity.SetTag("event_type", "orchestration_start");
            activity.SetTag("session_id", sessionId);
            activity.SetTag("request_type", requestType);
        }

        _logger.LogInformation("Orchestration started: {SessionId} | {RequestType} | {@LogData}",
            sessionId, requestType, logData);
    }

    /// <summary>
    /// 오케스트레이션 완료 로그
    /// </summary>
    public void LogOrchestrationComplete(string sessionId, bool success, TimeSpan duration, int stepCount, string? errorMessage = null)
    {
        using var activity = _activitySourceManager.StartOrchestrationActivity(sessionId, "orchestration_complete");
        
        var logData = new
        {
            EventType = "orchestration_complete",
            SessionId = sessionId,
            Success = success,
            Duration = duration.TotalMilliseconds,
            StepCount = stepCount,
            ErrorMessage = errorMessage,
            Timestamp = DateTimeOffset.UtcNow
        };

        if (activity != null)
        {
            activity.SetTag("event_type", "orchestration_complete");
            activity.SetTag("session_id", sessionId);
            activity.SetTag("success", success);
            activity.SetTag("duration_ms", duration.TotalMilliseconds);
            activity.SetTag("step_count", stepCount);
        }

        var logLevel = success ? LogLevel.Information : LogLevel.Warning;
        _logger.Log(logLevel, "Orchestration completed: {SessionId} | Success: {Success} | Duration: {Duration}ms | {@LogData}",
            sessionId, success, duration.TotalMilliseconds, logData);
    }

    /// <summary>
    /// LLM 호출 로그
    /// </summary>
    public void LogLLMCall(string provider, string model, string operation, TimeSpan responseTime, int? tokenCount = null, bool success = true, string? errorMessage = null)
    {
        using var activity = _activitySourceManager.StartLLMActivity(provider, model);
        
        var logData = new
        {
            EventType = "llm_call",
            Provider = provider,
            Model = model,
            Operation = operation,
            ResponseTime = responseTime.TotalMilliseconds,
            TokenCount = tokenCount,
            Success = success,
            ErrorMessage = errorMessage,
            Timestamp = DateTimeOffset.UtcNow
        };

        if (activity != null)
        {
            activity.SetTag("operation", operation);
            activity.SetTag("response_time_ms", responseTime.TotalMilliseconds);
            activity.SetTag("success", success);
            if (tokenCount.HasValue)
            {
                activity.SetTag("token_count", tokenCount.Value);
            }
        }

        var logLevel = success ? LogLevel.Information : LogLevel.Warning;
        _logger.Log(logLevel, "LLM call: {Provider}/{Model} | {Operation} | {ResponseTime}ms | {@LogData}",
            provider, model, operation, responseTime.TotalMilliseconds, logData);
    }

    /// <summary>
    /// 도구 실행 로그
    /// </summary>
    public void LogToolExecution(string toolName, string operation, TimeSpan executionTime, bool success = true, Dictionary<string, object>? parameters = null, string? errorMessage = null)
    {
        using var activity = _activitySourceManager.StartToolActivity(toolName, operation);
        
        var logData = new
        {
            EventType = "tool_execution",
            ToolName = toolName,
            Operation = operation,
            ExecutionTime = executionTime.TotalMilliseconds,
            Success = success,
            Parameters = parameters ?? new Dictionary<string, object>(),
            ErrorMessage = errorMessage,
            Timestamp = DateTimeOffset.UtcNow
        };

        if (activity != null)
        {
            activity.SetTag("execution_time_ms", executionTime.TotalMilliseconds);
            activity.SetTag("success", success);
        }

        var logLevel = success ? LogLevel.Information : LogLevel.Warning;
        _logger.Log(logLevel, "Tool execution: {ToolName} | {Operation} | {ExecutionTime}ms | {@LogData}",
            toolName, operation, executionTime.TotalMilliseconds, logData);
    }

    /// <summary>
    /// 상태 저장소 연산 로그
    /// </summary>
    public void LogStateOperation(string provider, string operation, string key, TimeSpan responseTime, bool success = true, string? errorMessage = null)
    {
        using var activity = _activitySourceManager.StartStateActivity(provider, operation, key);
        
        var logData = new
        {
            EventType = "state_operation",
            Provider = provider,
            Operation = operation,
            Key = key,
            ResponseTime = responseTime.TotalMilliseconds,
            Success = success,
            ErrorMessage = errorMessage,
            Timestamp = DateTimeOffset.UtcNow
        };

        if (activity != null)
        {
            activity.SetTag("response_time_ms", responseTime.TotalMilliseconds);
            activity.SetTag("success", success);
        }

        var logLevel = success ? LogLevel.Debug : LogLevel.Warning;
        _logger.Log(logLevel, "State operation: {Provider} | {Operation} | {Key} | {ResponseTime}ms | {@LogData}",
            provider, operation, key, responseTime.TotalMilliseconds, logData);
    }

    /// <summary>
    /// 성능 메트릭 로그
    /// </summary>
    public void LogPerformanceMetric(string metricName, double value, string unit, Dictionary<string, object>? tags = null)
    {
        var logData = new
        {
            EventType = "performance_metric",
            MetricName = metricName,
            Value = value,
            Unit = unit,
            Tags = tags ?? new Dictionary<string, object>(),
            Timestamp = DateTimeOffset.UtcNow
        };

        _logger.LogDebug("Performance metric: {MetricName} = {Value} {Unit} | {@LogData}",
            metricName, value, unit, logData);
    }

    /// <summary>
    /// 보안 이벤트 로그
    /// </summary>
    public void LogSecurityEvent(string eventType, string description, Dictionary<string, object>? context = null, bool isHighRisk = false)
    {
        var logData = new
        {
            EventType = "security_event",
            SecurityEventType = eventType,
            Description = description,
            Context = context ?? new Dictionary<string, object>(),
            IsHighRisk = isHighRisk,
            Timestamp = DateTimeOffset.UtcNow
        };

        var logLevel = isHighRisk ? LogLevel.Warning : LogLevel.Information;
        _logger.Log(logLevel, "Security event: {EventType} | {Description} | Risk: {IsHighRisk} | {@LogData}",
            eventType, description, isHighRisk, logData);
    }

    /// <summary>
    /// 사용자 정의 이벤트 로그
    /// </summary>
    public void LogCustomEvent(string eventType, string message, LogLevel logLevel = LogLevel.Information, Dictionary<string, object>? data = null)
    {
        var logData = new
        {
            EventType = eventType,
            Message = message,
            CustomData = data ?? new Dictionary<string, object>(),
            Timestamp = DateTimeOffset.UtcNow
        };

        _logger.Log(logLevel, "Custom event: {EventType} | {Message} | {@LogData}",
            eventType, message, logData);
    }

    /// <summary>
    /// 예외 로그
    /// </summary>
    public void LogException(Exception exception, string context, Dictionary<string, object>? additionalData = null)
    {
        var logData = new
        {
            EventType = "exception",
            Context = context,
            ExceptionType = exception.GetType().Name,
            Message = exception.Message,
            StackTrace = exception.StackTrace,
            AdditionalData = additionalData ?? new Dictionary<string, object>(),
            Timestamp = DateTimeOffset.UtcNow
        };

        _logger.LogError(exception, "Exception occurred: {Context} | {ExceptionType}: {Message} | {@LogData}",
            context, exception.GetType().Name, exception.Message, logData);
    }

    public void Dispose()
    {
        // 필요한 정리 작업 수행
    }
}