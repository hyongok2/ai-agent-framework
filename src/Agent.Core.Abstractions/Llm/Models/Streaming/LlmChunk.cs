using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using Agent.Core.Abstractions.Llm.Enums;
using Agent.Core.Abstractions.Llm.Models.Completion;
using Agent.Core.Abstractions.Llm.Models.Functions;
using Agent.Core.Abstractions.Streaming;
using Agent.Core.Abstractions.Streaming.Chunks;

namespace Agent.Core.Abstractions.Llm.Models.Streaming;

/// <summary>
/// LLM 스트리밍 청크
/// </summary>
public sealed record LlmChunk : StreamChunk
{
    /// <summary>
    /// 청크 타입
    /// </summary>
    public override string ChunkType => "llm";
    
    /// <summary>
    /// 모델명
    /// </summary>
    public required string Model { get; init; }
    
    /// <summary>
    /// 선택지 델타
    /// </summary>
    public IReadOnlyList<LlmChoiceDelta> ChoiceDeltas { get; init; } = Array.Empty<LlmChoiceDelta>();
    
    /// <summary>
    /// 사용량 정보 (완료 시에만)
    /// </summary>
    public LlmUsage? Usage { get; init; }
    
    /// <summary>
    /// 완료 이유 (완료 시에만)
    /// </summary>
    public FinishReason? FinishReason { get; init; }
    
    /// <summary>
    /// 함수 호출 정보 (해당 시에만)
    /// </summary>
    public FunctionCall? FunctionCall { get; init; }
    
    /// <summary>
    /// 공급자 특정 데이터
    /// </summary>
    public JsonNode? ProviderData { get; init; }
    
    /// <summary>
    /// 첫 번째 선택지의 텍스트 델타 (편의 속성)
    /// </summary>
    public string? ContentDelta => ChoiceDeltas.Count > 0 ? ChoiceDeltas[0].ContentDelta : null;
    
    /// <summary>
    /// 스트림 종료 여부
    /// </summary>
    public bool IsComplete { get; init; }
    
    /// <summary>
    /// 에러 정보 (에러 발생 시)
    /// </summary>
    public string? Error { get; init; }
    
    /// <summary>
    /// 에러 코드 (에러 발생 시)
    /// </summary>
    public string? ErrorCode { get; init; }
}