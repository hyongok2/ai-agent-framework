using System.Text.Json.Nodes;
using Agent.Abstractions.LLM.Enums;
using Agent.Abstractions.LLM.Models.Functions;

namespace Agent.Abstractions.LLM.Models.Completion;

/// <summary>
/// LLM 응답
/// </summary>
public sealed record LlmResponse
{
    /// <summary>
    /// 요청 ID
    /// </summary>
    public required string RequestId { get; init; }
    
    /// <summary>
    /// 모델명
    /// </summary>
    public required string Model { get; init; }
    
    /// <summary>
    /// 응답 선택지
    /// </summary>
    public required IReadOnlyList<LlmChoice> Choices { get; init; }
    
    /// <summary>
    /// 사용량 정보
    /// </summary>
    public LlmUsage? Usage { get; init; }
    
    /// <summary>
    /// 완료 이유
    /// </summary>
    public FinishReason FinishReason { get; init; }
    
    /// <summary>
    /// 생성 시간
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// 응답 시간 (밀리초)
    /// </summary>
    public long ResponseTimeMs { get; init; }
    
    /// <summary>
    /// 공급자 특정 데이터
    /// </summary>
    public JsonNode? ProviderData { get; init; }
    
    /// <summary>
    /// 메타데이터
    /// </summary>
    public IDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
    
    /// <summary>
    /// 첫 번째 선택지의 텍스트 (편의 속성)
    /// </summary>
    public string? Content => Choices.Count > 0 ? Choices[0].Message.Content : null;
    
    /// <summary>
    /// 첫 번째 선택지의 함수 호출 (편의 속성)
    /// </summary>
    public FunctionCall? FunctionCall => Choices.Count > 0 ? Choices[0].Message.FunctionCall : null;
}

