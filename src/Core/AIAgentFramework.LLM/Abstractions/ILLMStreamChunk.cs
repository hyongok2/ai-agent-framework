namespace AIAgentFramework.LLM.Abstractions;

/// <summary>
/// LLM 스트리밍 청크
/// 스트리밍 응답의 개별 조각
/// </summary>
public interface ILLMStreamChunk
{
    /// <summary>
    /// 청크 순서
    /// </summary>
    int Index { get; }

    /// <summary>
    /// 청크 내용
    /// </summary>
    string Content { get; }

    /// <summary>
    /// 스트리밍 완료 여부
    /// </summary>
    bool IsFinal { get; }

    /// <summary>
    /// 누적 토큰 사용량
    /// </summary>
    int AccumulatedTokens { get; }
}
