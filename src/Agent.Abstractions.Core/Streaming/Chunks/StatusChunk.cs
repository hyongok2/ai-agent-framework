namespace Agent.Abstractions.Core.Streaming.Chunks;

/// <summary>
/// 상태 업데이트 청크
/// </summary>
public sealed record StatusChunk : StreamChunk
{
    public required StatusType Status { get; init; }
    public string? Message { get; init; }
    public override string ChunkType => "status";
    
    /// <summary>
    /// 진행률 (0-100)
    /// </summary>
    public int? ProgressPercentage { get; init; }
}