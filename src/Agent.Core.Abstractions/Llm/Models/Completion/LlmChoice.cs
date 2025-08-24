using Agent.Core.Abstractions.Llm.Enums;

namespace Agent.Core.Abstractions.Llm.Models.Completion;

/// <summary>
/// LLM 선택지
/// </summary>
public sealed record LlmChoice
{
    /// <summary>
    /// 선택지 인덱스
    /// </summary>
    public int Index { get; init; }
    
    /// <summary>
    /// 메시지
    /// </summary>
    public required LlmMessage Message { get; init; }
    
    /// <summary>
    /// 완료 이유
    /// </summary>
    public FinishReason FinishReason { get; init; }
    
    /// <summary>
    /// 로그 확률
    /// </summary>
    public double? LogProb { get; init; }
    
    /// <summary>
    /// 델타 (스트리밍용)
    /// </summary>
    public LlmMessage? Delta { get; init; }
}