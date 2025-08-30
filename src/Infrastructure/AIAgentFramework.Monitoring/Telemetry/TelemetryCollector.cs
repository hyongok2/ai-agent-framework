using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace AIAgentFramework.Monitoring.Telemetry;

/// <summary>
/// OpenTelemetry 텔레메트리 수집기
/// </summary>
public class TelemetryCollector : IDisposable
{
    private readonly ILogger<TelemetryCollector> _logger;
    private readonly ActivitySourceManager _activitySourceManager;
    private readonly Meter _meter;
    private readonly Counter<long> _requestCounter;
    private readonly Counter<long> _errorCounter;
    private readonly Histogram<double> _requestDuration;
    private readonly Counter<long> _orchestrationCounter;
    private readonly Histogram<double> _llmResponseTime;
    private readonly Counter<long> _toolExecutionCounter;
    private readonly Histogram<double> _toolExecutionTime;

    public TelemetryCollector(
        ILogger<TelemetryCollector> logger,
        ActivitySourceManager activitySourceManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _activitySourceManager = activitySourceManager ?? throw new ArgumentNullException(nameof(activitySourceManager));

        // Meter 초기화
        _meter = new Meter("AIAgentFramework", "1.0.0");
        
        // 메트릭 초기화
        _requestCounter = _meter.CreateCounter<long>(
            "ai_agent_requests_total",
            "개",
            "총 요청 수");
            
        _errorCounter = _meter.CreateCounter<long>(
            "ai_agent_errors_total",
            "개",
            "총 오류 수");
            
        _requestDuration = _meter.CreateHistogram<double>(
            "ai_agent_request_duration_seconds",
            "초",
            "요청 처리 시간");
            
        _orchestrationCounter = _meter.CreateCounter<long>(
            "ai_agent_orchestration_executions_total",
            "개",
            "총 오케스트레이션 실행 수");
            
        _llmResponseTime = _meter.CreateHistogram<double>(
            "ai_agent_llm_response_time_seconds",
            "초",
            "LLM 응답 시간");
            
        _toolExecutionCounter = _meter.CreateCounter<long>(
            "ai_agent_tool_executions_total",
            "개",
            "총 도구 실행 수");
            
        _toolExecutionTime = _meter.CreateHistogram<double>(
            "ai_agent_tool_execution_time_seconds",
            "초",
            "도구 실행 시간");

        _logger.LogInformation("TelemetryCollector initialized with meter: {MeterName}", _meter.Name);
    }

    /// <summary>
    /// 요청 메트릭 기록
    /// </summary>
    /// <param name="operationType">작업 타입</param>
    /// <param name="success">성공 여부</param>
    /// <param name="duration">처리 시간</param>
    /// <param name="tags">추가 태그</param>
    public void RecordRequest(string operationType, bool success, TimeSpan duration, Dictionary<string, object>? tags = null)
    {
        var tagList = new TagList();
        tagList.Add("operation_type", operationType);
        tagList.Add("success", success);
        
        if (tags != null)
        {
            foreach (var tag in tags)
            {
                tagList.Add(tag.Key, tag.Value);
            }
        }

        _requestCounter.Add(1, tagList);
        _requestDuration.Record(duration.TotalSeconds, tagList);

        if (!success)
        {
            _errorCounter.Add(1, tagList);
        }

        _logger.LogDebug("Recorded request metric: {OperationType}, Success: {Success}, Duration: {Duration}ms",
            operationType, success, duration.TotalMilliseconds);
    }

    /// <summary>
    /// 오케스트레이션 실행 메트릭 기록
    /// </summary>
    /// <param name="sessionId">세션 ID</param>
    /// <param name="success">성공 여부</param>
    /// <param name="stepCount">실행 단계 수</param>
    public void RecordOrchestrationExecution(string sessionId, bool success, int stepCount)
    {
        var tags = new TagList
        {
            { "session_id", sessionId },
            { "success", success },
            { "step_count", stepCount }
        };

        _orchestrationCounter.Add(1, tags);

        _logger.LogDebug("Recorded orchestration execution: {SessionId}, Success: {Success}, Steps: {StepCount}",
            sessionId, success, stepCount);
    }

    /// <summary>
    /// LLM 응답 시간 메트릭 기록
    /// </summary>
    /// <param name="provider">LLM 제공자</param>
    /// <param name="model">모델명</param>
    /// <param name="responseTime">응답 시간</param>
    /// <param name="tokenCount">토큰 수</param>
    public void RecordLLMResponse(string provider, string model, TimeSpan responseTime, int? tokenCount = null)
    {
        var tags = new TagList
        {
            { "provider", provider },
            { "model", model }
        };

        if (tokenCount.HasValue)
        {
            tags.Add("token_count", tokenCount.Value);
        }

        _llmResponseTime.Record(responseTime.TotalSeconds, tags);

        _logger.LogDebug("Recorded LLM response: {Provider}/{Model}, Duration: {Duration}ms, Tokens: {TokenCount}",
            provider, model, responseTime.TotalMilliseconds, tokenCount);
    }

    /// <summary>
    /// 도구 실행 메트릭 기록
    /// </summary>
    /// <param name="toolName">도구명</param>
    /// <param name="success">성공 여부</param>
    /// <param name="executionTime">실행 시간</param>
    /// <param name="errorType">오류 타입 (실패 시)</param>
    public void RecordToolExecution(string toolName, bool success, TimeSpan executionTime, string? errorType = null)
    {
        var tags = new TagList
        {
            { "tool_name", toolName },
            { "success", success }
        };

        if (!success && !string.IsNullOrEmpty(errorType))
        {
            tags.Add("error_type", errorType);
        }

        _toolExecutionCounter.Add(1, tags);
        _toolExecutionTime.Record(executionTime.TotalSeconds, tags);

        _logger.LogDebug("Recorded tool execution: {ToolName}, Success: {Success}, Duration: {Duration}ms",
            toolName, success, executionTime.TotalMilliseconds);
    }

    /// <summary>
    /// 사용자 정의 메트릭 기록
    /// </summary>
    /// <param name="name">메트릭명</param>
    /// <param name="value">값</param>
    /// <param name="tags">태그</param>
    public void RecordCustomMetric(string name, double value, Dictionary<string, object>? tags = null)
    {
        var activity = _activitySourceManager.StartActivity($"custom_metric_{name}");
        
        if (activity != null)
        {
            activity.SetTag("metric_name", name);
            activity.SetTag("metric_value", value);
            
            if (tags != null)
            {
                foreach (var tag in tags)
                {
                    activity.SetTag(tag.Key, tag.Value?.ToString());
                }
            }
            
            activity.Stop();
        }

        _logger.LogDebug("Recorded custom metric: {MetricName} = {Value}", name, value);
    }

    /// <summary>
    /// 활성 상태 메트릭 반환
    /// </summary>
    public TelemetryStats GetCurrentStats()
    {
        return new TelemetryStats
        {
            MeterName = _meter.Name,
            MeterVersion = _meter.Version,
            IsActive = true,
            CollectionTime = DateTime.UtcNow
        };
    }

    public void Dispose()
    {
        _logger.LogInformation("Disposing TelemetryCollector");
        _meter?.Dispose();
    }
}

/// <summary>
/// 텔레메트리 통계 정보
/// </summary>
public class TelemetryStats
{
    public string MeterName { get; set; } = string.Empty;
    public string? MeterVersion { get; set; }
    public bool IsActive { get; set; }
    public DateTime CollectionTime { get; set; }
}