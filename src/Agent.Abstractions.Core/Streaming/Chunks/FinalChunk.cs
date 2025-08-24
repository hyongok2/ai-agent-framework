using Agent.Abstractions.Core.Streaming.Metrics;

namespace Agent.Abstractions.Core.Streaming.Chunks;

/// <summary>
/// 최종 결과 청크
/// </summary>
public sealed record FinalChunk : StreamChunk
{
    public required JsonDocument Result { get; init; }
    public bool Success { get; init; } = true;
    public string? Error { get; init; }
    public override string ChunkType => "final";
    
    /// <summary>
    /// 실행 메트릭
    /// </summary>
    public ExecutionMetrics? Metrics { get; init; }
}