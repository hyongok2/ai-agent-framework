using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace AIAgentFramework.Monitoring.Metrics;

/// <summary>
/// 성능 및 비즈니스 메트릭 수집기
/// </summary>
public class MetricsCollector : IDisposable
{
    private readonly Meter _meter;
    private readonly Counter<long> _requestCounter;
    private readonly Counter<long> _errorCounter;
    private readonly Histogram<double> _requestDuration;
    private readonly Histogram<long> _tokenUsage;
    private readonly ObservableGauge<long> _activeOrchestrations;
    private readonly Counter<long> _toolExecutions;
    private readonly Histogram<double> _llmResponseTime;
    private readonly Counter<decimal> _tokenCosts;
    
    private readonly ConcurrentDictionary<string, long> _activeOrchestrationsByType = new();
    private readonly ConcurrentDictionary<string, MetricSnapshot> _lastSnapshots = new();
    
    public MetricsCollector()
    {
        _meter = new Meter("AIAgentFramework.Monitoring", "1.0.0");
        
        // 요청 및 오류 카운터
        _requestCounter = _meter.CreateCounter<long>(
            "agent_requests_total",
            "개",
            "처리된 요청의 총 개수");
            
        _errorCounter = _meter.CreateCounter<long>(
            "agent_errors_total", 
            "개",
            "발생한 오류의 총 개수");
        
        // 성능 히스토그램
        _requestDuration = _meter.CreateHistogram<double>(
            "agent_request_duration",
            "ms", 
            "요청 처리 시간");
            
        _tokenUsage = _meter.CreateHistogram<long>(
            "agent_token_usage",
            "개",
            "LLM 토큰 사용량");
            
        _llmResponseTime = _meter.CreateHistogram<double>(
            "agent_llm_response_time",
            "ms",
            "LLM 응답 시간");
        
        // 활성 오케스트레이션 게이지
        _activeOrchestrations = _meter.CreateObservableGauge<long>(
            "agent_active_orchestrations",
            observeValue: () => GetActiveOrchestrationsCount(),
            "개",
            "현재 실행 중인 오케스트레이션 수");
        
        // 도구 실행 카운터
        _toolExecutions = _meter.CreateCounter<long>(
            "agent_tool_executions_total",
            "개",
            "실행된 도구의 총 개수");
            
        // 비용 추적
        _tokenCosts = _meter.CreateCounter<decimal>(
            "agent_token_costs_total",
            "USD",
            "토큰 사용 비용");
    }
    
    /// <summary>
    /// 오케스트레이션 요청 메트릭 기록
    /// </summary>
    public void RecordRequest(string requestType, bool success, TimeSpan duration, string? sessionId = null)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("request_type", requestType),
            new("success", success),
            new("session_id", sessionId)
        };
        
        _requestCounter.Add(1, tags);
        
        if (!success)
        {
            _errorCounter.Add(1, tags);
        }
        
        _requestDuration.Record(duration.TotalMilliseconds, tags);
        
        // 스냅샷 업데이트
        var key = $"request_{requestType}";
        _lastSnapshots.AddOrUpdate(key, 
            new MetricSnapshot(requestType, 1, duration.TotalMilliseconds),
            (_, existing) => existing.Update(1, duration.TotalMilliseconds));
    }
    
    /// <summary>
    /// LLM 호출 메트릭 기록
    /// </summary>
    public void RecordLLMCall(string provider, string model, TimeSpan responseTime, int tokenCount, decimal cost, bool success = true)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("provider", provider),
            new("model", model),
            new("success", success)
        };
        
        _llmResponseTime.Record(responseTime.TotalMilliseconds, tags);
        _tokenUsage.Record(tokenCount, tags);
        _tokenCosts.Add(cost, tags);
        
        if (!success)
        {
            _errorCounter.Add(1, tags.Concat(new[] { new KeyValuePair<string, object?>("component", "llm") }).ToArray());
        }
    }
    
    /// <summary>
    /// 도구 실행 메트릭 기록
    /// </summary>
    public void RecordToolExecution(string toolName, string operation, TimeSpan executionTime, bool success = true)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("tool_name", toolName),
            new("operation", operation),
            new("success", success)
        };
        
        _toolExecutions.Add(1, tags);
        _requestDuration.Record(executionTime.TotalMilliseconds, tags);
        
        if (!success)
        {
            _errorCounter.Add(1, tags.Concat(new[] { new KeyValuePair<string, object?>("component", "tool") }).ToArray());
        }
    }
    
    /// <summary>
    /// 활성 오케스트레이션 추가
    /// </summary>
    public void AddActiveOrchestration(string orchestrationType)
    {
        _activeOrchestrationsByType.AddOrUpdate(orchestrationType, 1, (_, count) => count + 1);
        UpdateActiveOrchestrationGauge();
    }
    
    /// <summary>
    /// 활성 오케스트레이션 제거
    /// </summary>
    public void RemoveActiveOrchestration(string orchestrationType)
    {
        _activeOrchestrationsByType.AddOrUpdate(orchestrationType, 0, (_, count) => Math.Max(0, count - 1));
        UpdateActiveOrchestrationGauge();
    }
    
    /// <summary>
    /// 상태 제공자 메트릭 기록
    /// </summary>
    public void RecordStateOperation(string provider, string operation, TimeSpan responseTime, bool success = true)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("provider", provider),
            new("operation", operation),
            new("success", success)
        };
        
        _requestDuration.Record(responseTime.TotalMilliseconds, tags);
        
        if (!success)
        {
            _errorCounter.Add(1, tags.Concat(new[] { new KeyValuePair<string, object?>("component", "state") }).ToArray());
        }
    }
    
    /// <summary>
    /// 사용자 정의 카운터 증가
    /// </summary>
    public void IncrementCustomCounter(string name, int value = 1, params KeyValuePair<string, object?>[] tags)
    {
        // 동적 카운터는 성능상 권장되지 않으므로 기본 요청 카운터 사용
        var allTags = tags.Concat(new[] { new KeyValuePair<string, object?>("custom_metric", name) }).ToArray();
        _requestCounter.Add(value, allTags);
    }
    
    /// <summary>
    /// 메트릭 스냅샷 가져오기
    /// </summary>
    public IReadOnlyDictionary<string, MetricSnapshot> GetSnapshots()
    {
        return _lastSnapshots.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }
    
    /// <summary>
    /// 현재 활성 오케스트레이션 수 가져오기
    /// </summary>
    public long GetActiveOrchestrationsCount()
    {
        return _activeOrchestrationsByType.Values.Sum();
    }
    
    /// <summary>
    /// 오케스트레이션 타입별 활성 수 가져오기
    /// </summary>
    public IReadOnlyDictionary<string, long> GetActiveOrchestrationsByType()
    {
        return _activeOrchestrationsByType.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }
    
    private void UpdateActiveOrchestrationGauge()
    {
        // ObservableGauge는 자동으로 값을 가져오므로 별도의 Record 호출이 필요하지 않음
        // GetActiveOrchestrationsCount() 메서드가 호출될 때 자동으로 현재 값을 반환함
    }
    
    public void Dispose()
    {
        _meter?.Dispose();
    }
}

/// <summary>
/// 메트릭 스냅샷 데이터
/// </summary>
public class MetricSnapshot
{
    private readonly object _lock = new();
    private long _count;
    private double _totalValue;
    private double _minValue;
    private double _maxValue;
    
    public string Name { get; }
    public DateTime LastUpdated { get; private set; }
    
    public long Count
    {
        get
        {
            lock (_lock) return _count;
        }
    }
    
    public double Average
    {
        get
        {
            lock (_lock) return _count > 0 ? _totalValue / _count : 0;
        }
    }
    
    public double Min
    {
        get
        {
            lock (_lock) return _minValue;
        }
    }
    
    public double Max
    {
        get
        {
            lock (_lock) return _maxValue;
        }
    }
    
    public double Total
    {
        get
        {
            lock (_lock) return _totalValue;
        }
    }
    
    internal MetricSnapshot(string name, long initialCount, double initialValue)
    {
        Name = name;
        _count = initialCount;
        _totalValue = initialValue;
        _minValue = initialValue;
        _maxValue = initialValue;
        LastUpdated = DateTime.UtcNow;
    }
    
    internal MetricSnapshot Update(long countDelta, double value)
    {
        lock (_lock)
        {
            _count += countDelta;
            _totalValue += value;
            _minValue = Math.Min(_minValue, value);
            _maxValue = Math.Max(_maxValue, value);
            LastUpdated = DateTime.UtcNow;
            return this;
        }
    }
}