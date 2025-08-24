using System.Text.Json.Nodes;
using Agent.Abstractions.LLM.Enums;
using Agent.Abstractions.Core.Streaming.Chunks;

namespace Agent.Abstractions.LLM.Models.Streaming;

/// <summary>
/// LLM 이벤트 청크 (상태 변경 등)
/// </summary>
public sealed record LlmEventChunk : StreamChunk
{
    /// <summary>
    /// 청크 타입
    /// </summary>
    public override string ChunkType => "llm_event";
    
    /// <summary>
    /// 이벤트 타입
    /// </summary>
    public required LlmEventType EventType { get; init; }
    
    /// <summary>
    /// 이벤트 데이터
    /// </summary>
    public JsonNode? EventData { get; init; }
    
    /// <summary>
    /// 메시지
    /// </summary>
    public string? Message { get; init; }
    
    /// <summary>
    /// 진행률 (0-100)
    /// </summary>
    public int? Progress { get; init; }
}