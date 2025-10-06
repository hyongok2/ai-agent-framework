namespace AIAgentFramework.Core.Abstractions;

/// <summary>
/// 로깅 추상화 인터페이스
/// LLM 및 Tool 실행 로그를 파일로 기록
/// </summary>
public interface ILogger
{
    /// <summary>
    /// 실행 로그를 파일에 기록
    /// </summary>
    /// <param name="logEntry">로그 엔트리</param>
    /// <param name="cancellationToken">취소 토큰</param>
    Task LogAsync(ILogEntry logEntry, CancellationToken cancellationToken = default);
}

/// <summary>
/// 로그 엔트리 인터페이스
/// </summary>
public interface ILogEntry
{
    /// <summary>로그 타입 (LLM, Tool)</summary>
    string LogType { get; }

    /// <summary>실행 대상 이름 (TaskPlanner, FileReader 등)</summary>
    string TargetName { get; }

    /// <summary>실행 ID</summary>
    string ExecutionId { get; }

    /// <summary>타임스탬프</summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>요청 데이터</summary>
    object? Request { get; }

    /// <summary>응답 데이터</summary>
    object? Response { get; }

    /// <summary>실행 시간 (밀리초)</summary>
    long DurationMs { get; }

    /// <summary>성공 여부</summary>
    bool Success { get; }

    /// <summary>오류 메시지 (실패 시)</summary>
    string? ErrorMessage { get; }
}
