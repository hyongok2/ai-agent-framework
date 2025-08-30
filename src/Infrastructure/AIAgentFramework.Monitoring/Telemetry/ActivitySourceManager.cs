using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AIAgentFramework.Monitoring.Telemetry;

/// <summary>
/// ActivitySource 관리자 - 분산 추적을 위한 Activity 생성 및 관리
/// </summary>
public class ActivitySourceManager : IDisposable
{
    private readonly ILogger<ActivitySourceManager> _logger;
    private readonly ActivitySource _activitySource;
    private readonly Dictionary<string, ActivitySource> _customSources;

    public const string DefaultActivitySourceName = "AIAgentFramework";
    public const string Version = "1.0.0";

    public ActivitySourceManager(ILogger<ActivitySourceManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _activitySource = new ActivitySource(DefaultActivitySourceName, Version);
        _customSources = new Dictionary<string, ActivitySource>();

        _logger.LogInformation("ActivitySourceManager initialized with source: {SourceName} v{Version}",
            DefaultActivitySourceName, Version);
    }

    /// <summary>
    /// 기본 Activity 시작
    /// </summary>
    /// <param name="name">Activity 이름</param>
    /// <param name="kind">Activity 종류</param>
    /// <returns>시작된 Activity 또는 null</returns>
    public Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal)
    {
        var activity = _activitySource.StartActivity(name, kind);
        
        if (activity != null)
        {
            activity.SetTag("framework", "AIAgentFramework");
            activity.SetTag("version", Version);
            activity.SetTag("timestamp", DateTimeOffset.UtcNow.ToString("O"));
            
            _logger.LogTrace("Started activity: {ActivityName} ({ActivityId})", name, activity.Id);
        }
        else
        {
            _logger.LogTrace("Activity not started (no listener): {ActivityName}", name);
        }

        return activity;
    }

    /// <summary>
    /// 오케스트레이션 Activity 시작
    /// </summary>
    /// <param name="sessionId">세션 ID</param>
    /// <param name="operationType">작업 타입</param>
    /// <returns>시작된 Activity 또는 null</returns>
    public Activity? StartOrchestrationActivity(string sessionId, string operationType)
    {
        var activity = StartActivity($"orchestration.{operationType}", ActivityKind.Internal);
        
        if (activity != null)
        {
            activity.SetTag("session_id", sessionId);
            activity.SetTag("operation_type", operationType);
            activity.SetTag("component", "orchestration");
            
            _logger.LogTrace("Started orchestration activity: {SessionId}/{OperationType}", sessionId, operationType);
        }

        return activity;
    }

    /// <summary>
    /// LLM 호출 Activity 시작
    /// </summary>
    /// <param name="provider">LLM 제공자</param>
    /// <param name="model">모델명</param>
    /// <param name="prompt">프롬프트 (로깅용 - 요약)</param>
    /// <returns>시작된 Activity 또는 null</returns>
    public Activity? StartLLMActivity(string provider, string model, string? prompt = null)
    {
        var activity = StartActivity($"llm.generate", ActivityKind.Client);
        
        if (activity != null)
        {
            activity.SetTag("llm.provider", provider);
            activity.SetTag("llm.model", model);
            activity.SetTag("component", "llm");
            
            if (!string.IsNullOrEmpty(prompt))
            {
                // 프롬프트 요약 (보안을 위해 처음 100자만)
                var promptSummary = prompt.Length > 100 ? prompt[..100] + "..." : prompt;
                activity.SetTag("llm.prompt_summary", promptSummary);
                activity.SetTag("llm.prompt_length", prompt.Length);
            }
            
            _logger.LogTrace("Started LLM activity: {Provider}/{Model}", provider, model);
        }

        return activity;
    }

    /// <summary>
    /// 도구 실행 Activity 시작
    /// </summary>
    /// <param name="toolName">도구명</param>
    /// <param name="operation">작업</param>
    /// <returns>시작된 Activity 또는 null</returns>
    public Activity? StartToolActivity(string toolName, string operation)
    {
        var activity = StartActivity($"tool.{operation}", ActivityKind.Internal);
        
        if (activity != null)
        {
            activity.SetTag("tool.name", toolName);
            activity.SetTag("tool.operation", operation);
            activity.SetTag("component", "tool");
            
            _logger.LogTrace("Started tool activity: {ToolName}/{Operation}", toolName, operation);
        }

        return activity;
    }

    /// <summary>
    /// State 작업 Activity 시작
    /// </summary>
    /// <param name="provider">State Provider 타입</param>
    /// <param name="operation">작업 (get, set, delete 등)</param>
    /// <param name="key">상태 키</param>
    /// <returns>시작된 Activity 또는 null</returns>
    public Activity? StartStateActivity(string provider, string operation, string key)
    {
        var activity = StartActivity($"state.{operation}", ActivityKind.Internal);
        
        if (activity != null)
        {
            activity.SetTag("state.provider", provider);
            activity.SetTag("state.operation", operation);
            activity.SetTag("state.key", key);
            activity.SetTag("component", "state");
            
            _logger.LogTrace("Started state activity: {Provider}/{Operation}/{Key}", provider, operation, key);
        }

        return activity;
    }

    /// <summary>
    /// Health Check Activity 시작
    /// </summary>
    /// <param name="checkName">헬스체크 이름</param>
    /// <returns>시작된 Activity 또는 null</returns>
    public Activity? StartHealthCheckActivity(string checkName)
    {
        var activity = StartActivity($"health.check", ActivityKind.Internal);
        
        if (activity != null)
        {
            activity.SetTag("health.check_name", checkName);
            activity.SetTag("component", "health");
            
            _logger.LogTrace("Started health check activity: {CheckName}", checkName);
        }

        return activity;
    }

    /// <summary>
    /// Activity에 성공/실패 상태 설정
    /// </summary>
    /// <param name="activity">Activity</param>
    /// <param name="success">성공 여부</param>
    /// <param name="error">오류 정보 (실패 시)</param>
    public static void SetActivityResult(Activity? activity, bool success, Exception? error = null)
    {
        if (activity == null) return;

        activity.SetTag("success", success);
        
        if (success)
        {
            activity.SetStatus(ActivityStatusCode.Ok);
        }
        else
        {
            activity.SetStatus(ActivityStatusCode.Error, error?.Message ?? "Operation failed");
            
            if (error != null)
            {
                activity.SetTag("error.type", error.GetType().Name);
                activity.SetTag("error.message", error.Message);
                
                if (!string.IsNullOrEmpty(error.StackTrace))
                {
                    // 스택 트레이스 요약 (처음 500자만)
                    var stackTrace = error.StackTrace.Length > 500 ? error.StackTrace[..500] + "..." : error.StackTrace;
                    activity.SetTag("error.stack_trace", stackTrace);
                }
            }
        }
    }

    /// <summary>
    /// Activity에 사용자 정의 태그 추가
    /// </summary>
    /// <param name="activity">Activity</param>
    /// <param name="tags">태그들</param>
    public static void AddTags(Activity? activity, Dictionary<string, object> tags)
    {
        if (activity == null || tags == null) return;

        foreach (var tag in tags)
        {
            activity.SetTag(tag.Key, tag.Value?.ToString());
        }
    }

    /// <summary>
    /// Activity에 이벤트 추가
    /// </summary>
    /// <param name="activity">Activity</param>
    /// <param name="name">이벤트 이름</param>
    /// <param name="timestamp">타임스탬프 (선택)</param>
    /// <param name="tags">추가 태그 (선택)</param>
    public static void AddEvent(Activity? activity, string name, DateTimeOffset? timestamp = null, Dictionary<string, object>? tags = null)
    {
        if (activity == null) return;

        var activityEvent = new ActivityEvent(name, timestamp ?? DateTimeOffset.UtcNow);
        
        if (tags != null)
        {
            var tagCollection = new ActivityTagsCollection();
            foreach (var tag in tags)
            {
                tagCollection.Add(tag.Key, tag.Value?.ToString());
            }
            activityEvent = new ActivityEvent(name, timestamp ?? DateTimeOffset.UtcNow, tagCollection);
        }

        activity.AddEvent(activityEvent);
    }

    /// <summary>
    /// 사용자 정의 ActivitySource 등록
    /// </summary>
    /// <param name="name">소스명</param>
    /// <param name="version">버전</param>
    /// <returns>등록된 ActivitySource</returns>
    public ActivitySource RegisterCustomSource(string name, string version = Version)
    {
        if (_customSources.ContainsKey(name))
        {
            return _customSources[name];
        }

        var customSource = new ActivitySource(name, version);
        _customSources[name] = customSource;

        _logger.LogInformation("Registered custom ActivitySource: {SourceName} v{Version}", name, version);
        return customSource;
    }

    /// <summary>
    /// 기본 ActivitySource 반환
    /// </summary>
    public ActivitySource GetDefaultSource() => _activitySource;

    /// <summary>
    /// 사용자 정의 ActivitySource 반환
    /// </summary>
    /// <param name="name">소스명</param>
    /// <returns>ActivitySource 또는 null</returns>
    public ActivitySource? GetCustomSource(string name)
    {
        _customSources.TryGetValue(name, out var source);
        return source;
    }

    /// <summary>
    /// 모든 등록된 ActivitySource 목록 반환
    /// </summary>
    public IEnumerable<(string Name, string Version)> GetAllSources()
    {
        yield return (DefaultActivitySourceName, Version);
        
        foreach (var kvp in _customSources)
        {
            yield return (kvp.Key, kvp.Value.Version ?? "Unknown");
        }
    }

    public void Dispose()
    {
        _logger.LogInformation("Disposing ActivitySourceManager");
        
        _activitySource?.Dispose();
        
        foreach (var source in _customSources.Values)
        {
            source?.Dispose();
        }
        
        _customSources.Clear();
    }
}