using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace AIAgentFramework.Monitoring.Tracing;

/// <summary>
/// 트레이스 샘플링 정책 관리
/// </summary>
public class TracingSampler
{
    private readonly ILogger<TracingSampler> _logger;
    private readonly TracingSamplerOptions _options;
    private readonly Random _random;
    
    public TracingSampler(ILogger<TracingSampler> logger, TracingSamplerOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _random = new Random();
    }
    
    /// <summary>
    /// 샘플링 여부 결정
    /// </summary>
    public SamplingDecision ShouldSample(ActivityContext parentContext, ActivityTraceId traceId, string activityName, ActivityKind kind, IEnumerable<KeyValuePair<string, object?>>? tags = null, IEnumerable<ActivityLink>? links = null)
    {
        try
        {
            // 1. 부모 컨텍스트에서 이미 샘플링된 경우 상속
            if (parentContext.TraceId != default && parentContext.TraceFlags.HasFlag(ActivityTraceFlags.Recorded))
            {
                _logger.LogTrace("부모 컨텍스트에서 샘플링 상속: {TraceId}", traceId);
                return new SamplingDecision(true, "부모에서 상속");
            }
            
            // 2. 강제 샘플링 태그 확인
            if (tags?.Any(t => t.Key == "force.sample" && t.Value?.ToString()?.ToLowerInvariant() == "true") == true)
            {
                _logger.LogTrace("강제 샘플링 태그로 인한 샘플링: {TraceId}", traceId);
                return new SamplingDecision(true, "강제 샘플링");
            }
            
            // 3. Activity 종류별 샘플링 정책
            var kindSamplingRate = GetSamplingRateForKind(kind);
            
            // 4. Activity 이름별 샘플링 정책
            var nameSamplingRate = GetSamplingRateForName(activityName);
            
            // 5. 최종 샘플링 비율 계산 (더 높은 비율 적용)
            var finalSamplingRate = Math.Max(kindSamplingRate, nameSamplingRate);
            
            // 6. 확률적 샘플링 실행
            var shouldSample = _random.NextDouble() < finalSamplingRate;
            
            var reason = shouldSample
                ? $"확률적 샘플링 성공 (비율: {finalSamplingRate:P})"
                : $"확률적 샘플링 제외 (비율: {finalSamplingRate:P})";
                
            _logger.LogTrace("샘플링 결정: {Decision}, 이유: {Reason}, TraceId: {TraceId}",
                shouldSample ? "샘플" : "제외", reason, traceId);
                
            return new SamplingDecision(shouldSample, reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "샘플링 결정 중 오류 발생, 기본 정책 적용: {TraceId}", traceId);
            return new SamplingDecision(true, "오류로 인한 기본 샘플링");
        }
    }
    
    /// <summary>
    /// Activity 종류별 샘플링 비율 가져오기
    /// </summary>
    private double GetSamplingRateForKind(ActivityKind kind)
    {
        return kind switch
        {
            ActivityKind.Server => _options.ServerSamplingRate,
            ActivityKind.Client => _options.ClientSamplingRate,
            ActivityKind.Producer => _options.ProducerSamplingRate,
            ActivityKind.Consumer => _options.ConsumerSamplingRate,
            ActivityKind.Internal => _options.InternalSamplingRate,
            _ => _options.DefaultSamplingRate
        };
    }
    
    /// <summary>
    /// Activity 이름별 샘플링 비율 가져오기
    /// </summary>
    private double GetSamplingRateForName(string activityName)
    {
        // 정확한 이름 매치
        if (_options.NameBasedSamplingRates.TryGetValue(activityName, out var exactRate))
        {
            return exactRate;
        }
        
        // 패턴 매치
        foreach (var pattern in _options.PatternBasedSamplingRates)
        {
            if (IsPatternMatch(activityName, pattern.Key))
            {
                return pattern.Value;
            }
        }
        
        return _options.DefaultSamplingRate;
    }
    
    /// <summary>
    /// 단순 패턴 매칭 (와일드카드 지원)
    /// </summary>
    private static bool IsPatternMatch(string input, string pattern)
    {
        // 간단한 와일드카드 매칭 구현
        if (pattern.Contains('*'))
        {
            var parts = pattern.Split('*', StringSplitOptions.RemoveEmptyEntries);
            var currentPos = 0;
            
            foreach (var part in parts)
            {
                var index = input.IndexOf(part, currentPos, StringComparison.OrdinalIgnoreCase);
                if (index == -1)
                    return false;
                currentPos = index + part.Length;
            }
            return true;
        }
        
        return input.Equals(pattern, StringComparison.OrdinalIgnoreCase);
    }
    
    /// <summary>
    /// 현재 샘플링 통계 가져오기
    /// </summary>
    public SamplingStatistics GetStatistics()
    {
        return new SamplingStatistics
        {
            DefaultSamplingRate = _options.DefaultSamplingRate,
            ServerSamplingRate = _options.ServerSamplingRate,
            ClientSamplingRate = _options.ClientSamplingRate,
            InternalSamplingRate = _options.InternalSamplingRate,
            NameBasedRulesCount = _options.NameBasedSamplingRates.Count,
            PatternBasedRulesCount = _options.PatternBasedSamplingRates.Count
        };
    }
    
    /// <summary>
    /// 런타임에 샘플링 비율 업데이트
    /// </summary>
    public void UpdateSamplingRate(ActivityKind kind, double rate)
    {
        if (rate < 0.0 || rate > 1.0)
            throw new ArgumentOutOfRangeException(nameof(rate), "샘플링 비율은 0.0에서 1.0 사이여야 합니다.");
            
        switch (kind)
        {
            case ActivityKind.Server:
                _options.ServerSamplingRate = rate;
                break;
            case ActivityKind.Client:
                _options.ClientSamplingRate = rate;
                break;
            case ActivityKind.Producer:
                _options.ProducerSamplingRate = rate;
                break;
            case ActivityKind.Consumer:
                _options.ConsumerSamplingRate = rate;
                break;
            case ActivityKind.Internal:
                _options.InternalSamplingRate = rate;
                break;
        }
        
        _logger.LogInformation("샘플링 비율 업데이트: {Kind} = {Rate:P}", kind, rate);
    }
    
    /// <summary>
    /// Activity 이름별 샘플링 비율 업데이트
    /// </summary>
    public void UpdateSamplingRate(string activityName, double rate)
    {
        if (rate < 0.0 || rate > 1.0)
            throw new ArgumentOutOfRangeException(nameof(rate), "샘플링 비율은 0.0에서 1.0 사이여야 합니다.");
            
        _options.NameBasedSamplingRates[activityName] = rate;
        _logger.LogInformation("이름별 샘플링 비율 업데이트: {ActivityName} = {Rate:P}", activityName, rate);
    }
}

/// <summary>
/// 트레이스 샘플링 옵션
/// </summary>
public class TracingSamplerOptions
{
    /// <summary>
    /// 기본 샘플링 비율
    /// </summary>
    public double DefaultSamplingRate { get; set; } = 1.0;
    
    /// <summary>
    /// 서버 Activity 샘플링 비율
    /// </summary>
    public double ServerSamplingRate { get; set; } = 1.0;
    
    /// <summary>
    /// 클라이언트 Activity 샘플링 비율
    /// </summary>
    public double ClientSamplingRate { get; set; } = 0.1;
    
    /// <summary>
    /// Producer Activity 샘플링 비율
    /// </summary>
    public double ProducerSamplingRate { get; set; } = 0.1;
    
    /// <summary>
    /// Consumer Activity 샘플링 비율
    /// </summary>
    public double ConsumerSamplingRate { get; set; } = 0.1;
    
    /// <summary>
    /// 내부 Activity 샘플링 비율
    /// </summary>
    public double InternalSamplingRate { get; set; } = 0.05;
    
    /// <summary>
    /// Activity 이름별 샘플링 비율
    /// </summary>
    public Dictionary<string, double> NameBasedSamplingRates { get; set; } = new()
    {
        ["orchestration.execute"] = 1.0,
        ["llm.generate"] = 0.5,
        ["tool.execute"] = 0.3,
        ["state.get"] = 0.1,
        ["state.set"] = 0.1,
        ["health.check"] = 0.01
    };
    
    /// <summary>
    /// 패턴별 샘플링 비율 (와일드카드 지원)
    /// </summary>
    public Dictionary<string, double> PatternBasedSamplingRates { get; set; } = new()
    {
        ["orchestration.*"] = 1.0,
        ["llm.*"] = 0.5,
        ["tool.*"] = 0.3,
        ["state.*"] = 0.1,
        ["*.error"] = 1.0,
        ["*.critical"] = 1.0
    };
}

/// <summary>
/// 샘플링 결정 결과
/// </summary>
public record SamplingDecision(bool ShouldSample, string Reason);

/// <summary>
/// 샘플링 통계
/// </summary>
public record SamplingStatistics
{
    public double DefaultSamplingRate { get; init; }
    public double ServerSamplingRate { get; init; }
    public double ClientSamplingRate { get; init; }
    public double InternalSamplingRate { get; init; }
    public int NameBasedRulesCount { get; init; }
    public int PatternBasedRulesCount { get; init; }
}