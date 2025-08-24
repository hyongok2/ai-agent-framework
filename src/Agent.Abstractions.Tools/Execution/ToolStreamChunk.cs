using System.Text.Json.Nodes;

namespace Agent.Abstractions.Tools.Execution;

/// <summary>
/// 도구 스트리밍 청크
/// </summary>
public sealed record ToolStreamChunk
{
    /// <summary>
    /// 청크 타입
    /// </summary>
    public required ToolChunkType Type { get; init; }
    
    /// <summary>
    /// 청크 데이터
    /// </summary>
    public JsonNode? Data { get; init; }
    
    /// <summary>
    /// 텍스트 데이터 (Type이 Text인 경우)
    /// </summary>
    public string? Text { get; init; }
    
    /// <summary>
    /// 진행률 (0-100)
    /// </summary>
    public int? ProgressPercentage { get; init; }
    
    /// <summary>
    /// 상태 메시지
    /// </summary>
    public string? StatusMessage { get; init; }
    
    /// <summary>
    /// 완료 여부
    /// </summary>
    public bool IsComplete { get; init; }
    
    /// <summary>
    /// 에러 정보
    /// </summary>
    public string? Error { get; init; }
}