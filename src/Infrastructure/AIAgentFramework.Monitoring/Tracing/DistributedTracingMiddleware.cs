using AIAgentFramework.Monitoring.Telemetry;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AIAgentFramework.Monitoring.Tracing;

/// <summary>
/// 분산 추적을 위한 미들웨어
/// </summary>
public class DistributedTracingMiddleware
{
    private readonly ActivitySourceManager _activitySourceManager;
    private readonly ILogger<DistributedTracingMiddleware> _logger;
    private readonly DistributedTracingOptions _options;
    
    public DistributedTracingMiddleware(
        ActivitySourceManager activitySourceManager,
        ILogger<DistributedTracingMiddleware> logger,
        DistributedTracingOptions options)
    {
        _activitySourceManager = activitySourceManager ?? throw new ArgumentNullException(nameof(activitySourceManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }
    
    /// <summary>
    /// 오케스트레이션 실행을 분산 추적으로 래핑
    /// </summary>
    public async Task<T> TraceOrchestrationAsync<T>(
        string operationName,
        string sessionId,
        Func<Activity?, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        using var activity = _activitySourceManager.StartOrchestrationActivity(sessionId, operationName);
        
        try
        {
            // 트레이스 컨텍스트 설정
            if (activity != null)
            {
                activity.SetTag("operation.name", operationName);
                activity.SetTag("session.id", sessionId);
                activity.SetTag("service.name", _options.ServiceName);
                activity.SetTag("service.version", _options.ServiceVersion);
                
                _logger.LogDebug("분산 추적 시작: {OperationName} (Session: {SessionId}, TraceId: {TraceId})",
                    operationName, sessionId, activity.TraceId);
            }
            
            var result = await operation(activity);
            
            if (activity != null)
            {
                activity.SetStatus(ActivityStatusCode.Ok);
                activity.SetTag("operation.result", "success");
                
                _logger.LogDebug("분산 추적 성공: {OperationName} (TraceId: {TraceId})",
                    operationName, activity.TraceId);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            if (activity != null)
            {
                activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity.SetTag("operation.result", "error");
                activity.SetTag("error.type", ex.GetType().Name);
                activity.SetTag("error.message", ex.Message);
                
                if (_options.IncludeStackTraces)
                {
                    activity.SetTag("error.stack_trace", ex.StackTrace);
                }
                
                _logger.LogError(ex, "분산 추적 오류: {OperationName} (TraceId: {TraceId})",
                    operationName, activity.TraceId);
            }
            
            throw;
        }
    }
    
    /// <summary>
    /// LLM 호출을 분산 추적으로 래핑
    /// </summary>
    public async Task<T> TraceLLMCallAsync<T>(
        string provider,
        string model,
        string operationName,
        Func<Activity?, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        using var activity = _activitySourceManager.StartLLMActivity(provider, model);
        
        try
        {
            if (activity != null)
            {
                activity.SetTag("llm.provider", provider);
                activity.SetTag("llm.model", model);
                activity.SetTag("llm.operation", operationName);
                activity.SetTag("component.type", "llm");
            }
            
            var result = await operation(activity);
            
            if (activity != null)
            {
                activity.SetStatus(ActivityStatusCode.Ok);
                activity.SetTag("llm.result", "success");
            }
            
            return result;
        }
        catch (Exception ex)
        {
            if (activity != null)
            {
                activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity.SetTag("llm.result", "error");
                activity.SetTag("error.type", ex.GetType().Name);
                activity.SetTag("error.message", ex.Message);
            }
            
            throw;
        }
    }
    
    /// <summary>
    /// 도구 실행을 분산 추적으로 래핑
    /// </summary>
    public async Task<T> TraceToolExecutionAsync<T>(
        string toolName,
        string operationName,
        Func<Activity?, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        using var activity = _activitySourceManager.StartToolActivity(toolName, operationName);
        
        try
        {
            if (activity != null)
            {
                activity.SetTag("tool.name", toolName);
                activity.SetTag("tool.operation", operationName);
                activity.SetTag("component.type", "tool");
            }
            
            var result = await operation(activity);
            
            if (activity != null)
            {
                activity.SetStatus(ActivityStatusCode.Ok);
                activity.SetTag("tool.result", "success");
            }
            
            return result;
        }
        catch (Exception ex)
        {
            if (activity != null)
            {
                activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity.SetTag("tool.result", "error");
                activity.SetTag("error.type", ex.GetType().Name);
                activity.SetTag("error.message", ex.Message);
            }
            
            throw;
        }
    }
    
    /// <summary>
    /// 상태 저장소 연산을 분산 추적으로 래핑
    /// </summary>
    public async Task<T> TraceStateOperationAsync<T>(
        string provider,
        string operationName,
        string key,
        Func<Activity?, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        using var activity = _activitySourceManager.StartStateActivity(provider, operationName, key);
        
        try
        {
            if (activity != null)
            {
                activity.SetTag("state.provider", provider);
                activity.SetTag("state.operation", operationName);
                activity.SetTag("state.key", key);
                activity.SetTag("component.type", "state");
            }
            
            var result = await operation(activity);
            
            if (activity != null)
            {
                activity.SetStatus(ActivityStatusCode.Ok);
                activity.SetTag("state.result", "success");
            }
            
            return result;
        }
        catch (Exception ex)
        {
            if (activity != null)
            {
                activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity.SetTag("state.result", "error");
                activity.SetTag("error.type", ex.GetType().Name);
                activity.SetTag("error.message", ex.Message);
            }
            
            throw;
        }
    }
    
    /// <summary>
    /// 현재 트레이스 컨텍스트 정보 가져오기
    /// </summary>
    public TraceContext? GetCurrentTraceContext()
    {
        var currentActivity = Activity.Current;
        if (currentActivity == null)
            return null;
            
        return new TraceContext(
            currentActivity.TraceId.ToString(),
            currentActivity.SpanId.ToString(),
            currentActivity.ParentSpanId.ToString(),
            currentActivity.ActivityTraceFlags.HasFlag(ActivityTraceFlags.Recorded));
    }
    
    /// <summary>
    /// 트레이스 컨텍스트를 헤더로 내보내기
    /// </summary>
    public Dictionary<string, string> ExportTraceHeaders()
    {
        var headers = new Dictionary<string, string>();
        var currentActivity = Activity.Current;
        
        if (currentActivity != null)
        {
            // W3C Trace Context 표준 헤더
            headers["traceparent"] = $"00-{currentActivity.TraceId}-{currentActivity.SpanId}-{(currentActivity.ActivityTraceFlags.HasFlag(ActivityTraceFlags.Recorded) ? "01" : "00")}";
            
            // 커스텀 헤더
            headers["x-correlation-id"] = currentActivity.TraceId.ToString();
            headers["x-span-id"] = currentActivity.SpanId.ToString();
        }
        
        return headers;
    }
    
    /// <summary>
    /// 헤더에서 트레이스 컨텍스트 가져오기
    /// </summary>
    public void ImportTraceHeaders(Dictionary<string, string> headers)
    {
        if (headers.TryGetValue("traceparent", out var traceparent))
        {
            try
            {
                // W3C Trace Context 파싱
                var parts = traceparent.Split('-');
                if (parts.Length == 4)
                {
                    var traceId = ActivityTraceId.CreateFromString(parts[1]);
                    var spanId = ActivitySpanId.CreateFromString(parts[2]);
                    var traceFlags = parts[3] == "01" ? ActivityTraceFlags.Recorded : ActivityTraceFlags.None;
                    
                    var context = new ActivityContext(traceId, spanId, traceFlags);
                    Activity.Current = new Activity("imported").SetParentId(context.TraceId, context.SpanId, context.TraceFlags);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "트레이스 헤더 파싱 실패: {Traceparent}", traceparent);
            }
        }
    }
}

/// <summary>
/// 분산 추적 설정 옵션
/// </summary>
public class DistributedTracingOptions
{
    /// <summary>
    /// 서비스 이름
    /// </summary>
    public string ServiceName { get; set; } = "AIAgentFramework";
    
    /// <summary>
    /// 서비스 버전
    /// </summary>
    public string ServiceVersion { get; set; } = "1.0.0";
    
    /// <summary>
    /// 스택 트레이스 포함 여부
    /// </summary>
    public bool IncludeStackTraces { get; set; } = true;
    
    /// <summary>
    /// 샘플링 비율 (0.0 ~ 1.0)
    /// </summary>
    public double SamplingRatio { get; set; } = 1.0;
    
    /// <summary>
    /// 최대 추적 길이
    /// </summary>
    public int MaxTraceLength { get; set; } = 1000;
}

/// <summary>
/// 트레이스 컨텍스트 정보
/// </summary>
public record TraceContext(
    string TraceId,
    string SpanId,
    string ParentSpanId,
    bool IsRecorded);