using Agent.Core.Abstractions.Llm.Enums;

namespace Agent.Core.Abstractions.Llm.Models.Streaming;

/// <summary>
/// LLM 선택지 델타
/// </summary>
public sealed record LlmChoiceDelta
{
    /// <summary>
    /// 선택지 인덱스
    /// </summary>
    public int Index { get; init; }
    
    /// <summary>
    /// 내용 델타
    /// </summary>
    public string? ContentDelta { get; init; }
    
    /// <summary>
    /// 역할 (첫 청크에서만)
    /// </summary>
    public MessageRole? Role { get; init; }
    
    /// <summary>
    /// 함수 호출 델타
    /// </summary>
    public FunctionCallDelta? FunctionCallDelta { get; init; }
    
    /// <summary>
    /// 완료 이유 (완료 시에만)
    /// </summary>
    public FinishReason? FinishReason { get; init; }
    
    /// <summary>
    /// 로그 확률
    /// </summary>
    public double? LogProb { get; init; }
    
    /// <summary>
    /// 토큰 정보
    /// </summary>
    public TokenInfo? TokenInfo { get; init; }
}