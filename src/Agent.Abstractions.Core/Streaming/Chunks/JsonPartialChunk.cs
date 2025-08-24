namespace Agent.Abstractions.Core.Streaming.Chunks;

/// <summary>
/// JSON 부분 청크 (스트리밍 JSON 구성용)
/// </summary>
public sealed record JsonPartialChunk : StreamChunk
{
    public required string PartialJson { get; init; }
    public override string ChunkType => "json_partial";
    
    /// <summary>
    /// JSON 경로 (예: "result.items[0].name")
    /// </summary>
    public string? JsonPath { get; init; }
}