namespace Agent.Core.Abstractions.Streaming.Chunks;

/// <summary>
/// 에러 청크
/// </summary>
public sealed record ErrorChunk : StreamChunk
{
    public required string ErrorCode { get; init; }
    public required string Message { get; init; }
    public string? Details { get; init; }
    public override string ChunkType => "error";
    
    /// <summary>
    /// 재시도 가능 여부
    /// </summary>
    public bool IsRetryable { get; init; }
}