using AIAgentFramework.Core.Abstractions;

namespace AIAgentFramework.LLM.Abstractions;

/// <summary>
/// LLM 실행 결과 인터페이스
/// </summary>
public interface ILLMResult : IResult
{
    /// <summary>
    /// LLM 역할
    /// </summary>
    LLMRole Role { get; }

    /// <summary>
    /// 원본 응답 텍스트
    /// </summary>
    string RawResponse { get; }

    /// <summary>
    /// 파싱된 응답 데이터 (JSON 구조화)
    /// </summary>
    object? ParsedData { get; }

    /// <summary>
    /// 다음 실행할 액션 (있는 경우)
    /// </summary>
    string? NextAction { get; }

    /// <summary>
    /// 토큰 사용량
    /// </summary>
    int TokenUsage { get; }
}
