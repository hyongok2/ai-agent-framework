namespace AIAgentFramework.Core.Abstractions;

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
