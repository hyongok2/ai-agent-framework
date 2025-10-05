using AIAgentFramework.LLM.Abstractions;

namespace AIAgentFramework.LLM.Models;

/// <summary>
/// LLM 스트리밍 청크 구현체
/// </summary>
public class LLMStreamChunk : ILLMStreamChunk
{
    /// <summary>
    /// 청크 순서
    /// </summary>
    public int Index { get; init; }

    /// <summary>
    /// 청크 내용
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// 스트리밍 완료 여부
    /// </summary>
    public bool IsFinal { get; init; }

    /// <summary>
    /// 누적 토큰 사용량
    /// </summary>
    public int AccumulatedTokens { get; init; }
}
