namespace Agent.Abstractions.Tools.Metadata;

/// <summary>
/// 도구 기능 요구사항
/// </summary>
public sealed record ToolCapabilityRequirements
{
    /// <summary>
    /// 스트리밍 지원 필요
    /// </summary>
    public bool? RequiresStreaming { get; init; }
    
    /// <summary>
    /// 병렬 실행 지원 필요
    /// </summary>
    public bool? RequiresParallelExecution { get; init; }
    
    /// <summary>
    /// 재시도 가능성 필요
    /// </summary>
    public bool? RequiresRetryable { get; init; }
    
    /// <summary>
    /// 멱등성 필요
    /// </summary>
    public bool? RequiresIdempotent { get; init; }
    
    /// <summary>
    /// 캐시 가능성 필요
    /// </summary>
    public bool? RequiresCacheable { get; init; }
    
    /// <summary>
    /// 최대 허용 실행 시간
    /// </summary>
    public TimeSpan? MaxAllowedExecutionTime { get; init; }
}

