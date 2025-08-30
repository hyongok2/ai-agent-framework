using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace AIAgentFramework.Monitoring.Tracing;

/// <summary>
/// 마이크로서비스 간 트레이스 컨텍스트 전파 관리
/// </summary>
public class TraceContextPropagation
{
    private readonly ILogger<TraceContextPropagation> _logger;
    private const string TraceParentHeaderName = "traceparent";
    private const string TraceStateHeaderName = "tracestate";
    private const string CorrelationIdHeaderName = "x-correlation-id";
    
    public TraceContextPropagation(ILogger<TraceContextPropagation> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    /// <summary>
    /// 현재 Activity에서 HTTP 헤더 생성
    /// </summary>
    public Dictionary<string, string> CreatePropagationHeaders()
    {
        var headers = new Dictionary<string, string>();
        var currentActivity = Activity.Current;
        
        if (currentActivity == null)
        {
            _logger.LogDebug("현재 Activity가 없어 전파 헤더를 생성하지 않습니다.");
            return headers;
        }
        
        try
        {
            // W3C Trace Context 표준을 따르는 traceparent 헤더 생성
            var version = "00";
            var traceId = currentActivity.TraceId.ToString();
            var spanId = currentActivity.SpanId.ToString();
            var traceFlags = currentActivity.ActivityTraceFlags.HasFlag(ActivityTraceFlags.Recorded) ? "01" : "00";
            
            headers[TraceParentHeaderName] = $"{version}-{traceId}-{spanId}-{traceFlags}";
            
            // tracestate 헤더 (필요시)
            if (!string.IsNullOrEmpty(currentActivity.TraceStateString))
            {
                headers[TraceStateHeaderName] = currentActivity.TraceStateString;
            }
            
            // 추가 상관관계 ID
            headers[CorrelationIdHeaderName] = currentActivity.TraceId.ToString();
            
            // 커스텀 태그들을 헤더로 전파
            foreach (var tag in currentActivity.Tags.Where(t => t.Key.StartsWith("propagate.")))
            {
                var headerName = $"x-{tag.Key.Replace("propagate.", "").Replace(".", "-")}";
                headers[headerName] = tag.Value ?? string.Empty;
            }
            
            _logger.LogTrace("전파 헤더 생성 완료: TraceId={TraceId}, SpanId={SpanId}", traceId, spanId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "전파 헤더 생성 중 오류 발생");
        }
        
        return headers;
    }
    
    /// <summary>
    /// HTTP 헤더에서 트레이스 컨텍스트 추출
    /// </summary>
    public ActivityContext? ExtractContextFromHeaders(Dictionary<string, string> headers)
    {
        if (headers == null || !headers.TryGetValue(TraceParentHeaderName, out var traceparent))
        {
            _logger.LogTrace("traceparent 헤더가 없어 컨텍스트를 추출할 수 없습니다.");
            return null;
        }
        
        try
        {
            // traceparent 헤더 파싱: 00-traceId-spanId-flags
            var parts = traceparent.Split('-');
            if (parts.Length != 4 || parts[0] != "00")
            {
                _logger.LogWarning("유효하지 않은 traceparent 형식: {Traceparent}", traceparent);
                return null;
            }
            
            var traceId = ActivityTraceId.CreateFromString(parts[1]);
            var spanId = ActivitySpanId.CreateFromString(parts[2]);
            var traceFlags = parts[3] == "01" ? ActivityTraceFlags.Recorded : ActivityTraceFlags.None;
            
            // tracestate 처리
            string? traceState = null;
            if (headers.TryGetValue(TraceStateHeaderName, out var traceStateValue))
            {
                traceState = traceStateValue;
            }
            
            var context = new ActivityContext(traceId, spanId, traceFlags, traceState);
            
            _logger.LogTrace("컨텍스트 추출 완료: TraceId={TraceId}, SpanId={SpanId}", traceId, spanId);
            
            return context;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "트레이스 컨텍스트 추출 중 오류 발생: {Traceparent}", traceparent);
            return null;
        }
    }
    
    /// <summary>
    /// 새로운 Activity를 부모 컨텍스트와 함께 시작
    /// </summary>
    public Activity? StartActivityWithContext(string activityName, ActivityContext? parentContext, ActivityKind kind = ActivityKind.Internal)
    {
        var activitySource = new ActivitySource("AIAgentFramework.Distributed");
        
        if (parentContext.HasValue)
        {
            var activity = activitySource.StartActivity(activityName, kind, parentContext.Value);
            _logger.LogTrace("부모 컨텍스트와 함께 Activity 시작: {ActivityName}, ParentTraceId={TraceId}",
                activityName, parentContext.Value.TraceId);
            return activity;
        }
        else
        {
            var activity = activitySource.StartActivity(activityName, kind);
            _logger.LogTrace("새로운 루트 Activity 시작: {ActivityName}", activityName);
            return activity;
        }
    }
    
    /// <summary>
    /// HTTP 클라이언트 요청에 전파 헤더 추가
    /// </summary>
    public void InjectIntoHttpRequest(HttpRequestMessage request)
    {
        var headers = CreatePropagationHeaders();
        
        foreach (var (name, value) in headers)
        {
            try
            {
                request.Headers.Add(name, value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "HTTP 요청에 헤더 추가 실패: {HeaderName}={HeaderValue}", name, value);
            }
        }
        
        _logger.LogTrace("HTTP 요청에 {Count}개 전파 헤더 주입 완료", headers.Count);
    }
    
    /// <summary>
    /// HTTP 응답에서 전파 헤더 추출
    /// </summary>
    public ActivityContext? ExtractFromHttpResponse(HttpResponseMessage response)
    {
        var headers = new Dictionary<string, string>();
        
        // 응답 헤더에서 전파 헤더 추출
        foreach (var header in response.Headers.Concat(response.Content?.Headers ?? Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>()))
        {
            var value = header.Value?.FirstOrDefault();
            if (!string.IsNullOrEmpty(value))
            {
                headers[header.Key] = value;
            }
        }
        
        return ExtractContextFromHeaders(headers);
    }
    
    /// <summary>
    /// 현재 Activity에 전파 가능한 태그 추가
    /// </summary>
    public void AddPropagationTag(string key, string value)
    {
        var activity = Activity.Current;
        if (activity != null)
        {
            activity.SetTag($"propagate.{key}", value);
            _logger.LogTrace("전파 태그 추가: {Key}={Value}", key, value);
        }
    }
    
    /// <summary>
    /// 전파된 태그들을 현재 Activity에서 가져오기
    /// </summary>
    public Dictionary<string, string> GetPropagatedTags()
    {
        var activity = Activity.Current;
        if (activity == null)
            return new Dictionary<string, string>();
            
        return activity.Tags
            .Where(t => t.Key.StartsWith("propagate."))
            .ToDictionary(
                t => t.Key.Substring("propagate.".Length),
                t => t.Value ?? string.Empty);
    }
    
    /// <summary>
    /// 마이크로서비스 간 컨텍스트 유효성 검증
    /// </summary>
    public bool ValidateTraceContext(ActivityContext context)
    {
        try
        {
            // 기본 유효성 검사
            if (context.TraceId == default || context.SpanId == default)
            {
                _logger.LogWarning("유효하지 않은 트레이스 컨텍스트: TraceId 또는 SpanId가 기본값입니다.");
                return false;
            }
            
            // 트레이스 ID는 16바이트, 스팬 ID는 8바이트여야 함
            var traceIdString = context.TraceId.ToString();
            var spanIdString = context.SpanId.ToString();
            
            if (traceIdString.Length != 32 || spanIdString.Length != 16)
            {
                _logger.LogWarning("잘못된 트레이스 컨텍스트 형식: TraceId 길이={TraceIdLength}, SpanId 길이={SpanIdLength}",
                    traceIdString.Length, spanIdString.Length);
                return false;
            }
            
            _logger.LogTrace("트레이스 컨텍스트 유효성 검증 성공: TraceId={TraceId}", context.TraceId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "트레이스 컨텍스트 유효성 검증 중 오류");
            return false;
        }
    }
}

/// <summary>
/// 분산 추적 설정을 위한 확장 메서드
/// </summary>
public static class TraceContextPropagationExtensions
{
    /// <summary>
    /// HttpClient에 자동 추적 헤더 주입 설정
    /// </summary>
    public static HttpClient WithTracing(this HttpClient httpClient, TraceContextPropagation propagation)
    {
        // HttpClient 요청 전 이벤트 핸들러 등록은 .NET의 제약으로 인해 직접 지원하지 않음
        // 대신 사용자가 요청 시마다 수동으로 InjectIntoHttpRequest를 호출해야 함
        return httpClient;
    }
    
    /// <summary>
    /// Activity에 마이크로서비스 정보 태그 추가
    /// </summary>
    public static Activity? WithMicroserviceInfo(this Activity? activity, string serviceName, string serviceVersion, string environment = "development")
    {
        if (activity != null)
        {
            activity.SetTag("service.name", serviceName);
            activity.SetTag("service.version", serviceVersion);
            activity.SetTag("service.environment", environment);
        }
        return activity;
    }
}