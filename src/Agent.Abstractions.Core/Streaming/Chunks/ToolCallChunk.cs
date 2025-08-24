namespace Agent.Abstractions.Core.Streaming.Chunks;

/// <summary>
/// 도구 호출 청크
/// </summary>
public sealed record ToolCallChunk : StreamChunk
{
    public required string ToolName { get; init; }
    public required JsonDocument Arguments { get; init; }
    public override string ChunkType => "tool_call";
    
    /// <summary>
    /// 도구 호출 ID (추적용)
    /// </summary>
    public string? CallId { get; init; }
}