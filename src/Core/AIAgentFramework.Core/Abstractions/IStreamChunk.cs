namespace AIAgentFramework.Core.Abstractions;

/// <summary>
/// 스트리밍 청크 기본 인터페이스
/// </summary>
public interface IStreamChunk
{
    /// <summary>
    /// 청크 타입 (Text, ToolCall, Status 등)
    /// </summary>
    StreamChunkType Type { get; }

    /// <summary>
    /// 청크 내용
    /// </summary>
    string Content { get; }

    /// <summary>
    /// 스트리밍 완료 여부
    /// </summary>
    bool IsFinal { get; }

    /// <summary>
    /// 타임스탬프
    /// </summary>
    DateTimeOffset Timestamp { get; }
}

/// <summary>
/// 스트리밍 청크 타입
/// </summary>
public enum StreamChunkType
{
    /// <summary>
    /// 일반 텍스트
    /// </summary>
    Text,

    /// <summary>
    /// 도구 호출 시작
    /// </summary>
    ToolCallStart,

    /// <summary>
    /// 도구 호출 완료
    /// </summary>
    ToolCallComplete,

    /// <summary>
    /// 상태 업데이트
    /// </summary>
    Status,

    /// <summary>
    /// 오류
    /// </summary>
    Error,

    /// <summary>
    /// 완료
    /// </summary>
    Complete
}
