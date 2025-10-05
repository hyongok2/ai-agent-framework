namespace AIAgentFramework.Core.Abstractions;

/// <summary>
/// 모든 실행 결과의 기본 인터페이스
/// LLM 응답과 Tool 응답의 공통 구조
/// </summary>
public interface IResult
{
    /// <summary>
    /// 실행 성공 여부
    /// </summary>
    bool IsSuccess { get; }

    /// <summary>
    /// 오류 메시지 (실패 시)
    /// </summary>
    string? ErrorMessage { get; }

    /// <summary>
    /// 실행 시작 시간
    /// </summary>
    DateTimeOffset StartedAt { get; }

    /// <summary>
    /// 실행 완료 시간
    /// </summary>
    DateTimeOffset CompletedAt { get; }

    /// <summary>
    /// 실행 소요 시간
    /// </summary>
    TimeSpan Duration => CompletedAt - StartedAt;
}
