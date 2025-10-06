namespace AIAgentFramework.Core.Models;

/// <summary>
/// 채팅 메시지 모델
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// 메시지 ID
    /// </summary>
    public required string MessageId { get; init; }

    /// <summary>
    /// 세션 ID
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// 메시지 역할 (user, assistant, system)
    /// </summary>
    public required MessageRole Role { get; init; }

    /// <summary>
    /// 메시지 내용
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// 생성 시각
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// 메타데이터 (선택적)
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// 토큰 수 (선택적)
    /// </summary>
    public int? TokenCount { get; init; }
}

/// <summary>
/// 메시지 역할
/// </summary>
public enum MessageRole
{
    /// <summary>
    /// 사용자 메시지
    /// </summary>
    User,

    /// <summary>
    /// AI 어시스턴트 메시지
    /// </summary>
    Assistant,

    /// <summary>
    /// 시스템 메시지
    /// </summary>
    System
}
