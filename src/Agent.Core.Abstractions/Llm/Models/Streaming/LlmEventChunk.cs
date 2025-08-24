using System.Text.Json.Nodes;
using Agent.Core.Abstractions.Llm.Enums;
using Agent.Core.Abstractions.Streaming;
using Agent.Core.Abstractions.Streaming.Chunks;

namespace Agent.Core.Abstractions.Llm.Models.Streaming;

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