namespace Agent.Abstractions.Tools.Metadata;

/// <summary>
/// 도구 기능
/// </summary>
public sealed record ToolCapabilities
{
    /// <summary>
    /// 스트리밍 지원 여부
    /// </summary>
    public bool SupportsStreaming { get; init; }
    
    /// <summary>
    /// 병렬 실행 지원 여부
    /// </summary>
    public bool SupportsParallelExecution { get; init; } = true;
    
    /// <summary>
    /// 재시도 가능 여부
    /// </summary>
    public bool IsRetryable { get; init; } = true;
    
    /// <summary>
    /// 멱등성 여부 (동일 입력에 동일 출력)
    /// </summary>
    public bool IsIdempotent { get; init; } = true;
    
    /// <summary>
    /// 캐시 가능 여부
    /// </summary>
    public bool IsCacheable { get; init; } = true;
    
    /// <summary>
    /// 최대 실행 시간
    /// </summary>
    public TimeSpan? MaxExecutionTime { get; init; }
    
    /// <summary>
    /// 최대 입력 크기 (바이트)
    /// </summary>
    public long? MaxInputSize { get; init; }
    
    /// <summary>
    /// 최대 출력 크기 (바이트)
    /// </summary>
    public long? MaxOutputSize { get; init; }
}
