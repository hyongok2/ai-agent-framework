using System;
using System.Collections.Generic;

namespace Agent.Core.Abstractions.Tools.Execution;

/// <summary>
/// 도구 실행 메트릭
/// </summary>
public sealed record ToolExecutionMetrics
{
    /// <summary>
    /// 시작 시간
    /// </summary>
    public DateTimeOffset StartTime { get; init; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// 종료 시간
    /// </summary>
    public DateTimeOffset? EndTime { get; init; }
    
    /// <summary>
    /// 실행 시간
    /// </summary>
    public TimeSpan? Duration => EndTime - StartTime;
    
    /// <summary>
    /// 입력 크기 (바이트)
    /// </summary>
    public long? InputSize { get; init; }
    
    /// <summary>
    /// 출력 크기 (바이트)
    /// </summary>
    public long? OutputSize { get; init; }
    
    /// <summary>
    /// 메모리 사용량 (바이트)
    /// </summary>
    public long? MemoryUsed { get; init; }
    
    /// <summary>
    /// CPU 시간 (밀리초)
    /// </summary>
    public long? CpuTimeMs { get; init; }
    
    /// <summary>
    /// API 호출 수
    /// </summary>
    public int? ApiCalls { get; init; }
    
    /// <summary>
    /// 비용
    /// </summary>
    public decimal? Cost { get; init; }
    
    /// <summary>
    /// 캐시 히트 여부
    /// </summary>
    public bool? CacheHit { get; init; }
    
    /// <summary>
    /// 추가 메트릭
    /// </summary>
    public IDictionary<string, object> AdditionalMetrics { get; init; } = new Dictionary<string, object>();
}