namespace AIAgentFramework.Core.Abstractions;

/// <summary>
/// 실행 컨텍스트 기본 인터페이스
/// LLM과 Tool 실행 시 필요한 공통 컨텍스트 정보
/// </summary>
public interface IExecutionContext
{
    /// <summary>
    /// 고유 실행 ID
    /// </summary>
    string ExecutionId { get; }

    /// <summary>
    /// 사용자 ID
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// 세션 ID
    /// </summary>
    string SessionId { get; }

    /// <summary>
    /// 실행 타임스탬프
    /// </summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>
    /// 추가 메타데이터
    /// </summary>
    IReadOnlyDictionary<string, object> Metadata { get; }
}
