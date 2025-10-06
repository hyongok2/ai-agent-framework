namespace AIAgentFramework.Core.Models;

/// <summary>
/// 채팅 세션 모델
/// </summary>
public class ChatSession
{
    /// <summary>
    /// 세션 ID
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// 사용자 ID (선택적)
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// 세션 제목
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// 세션 생성 시각
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// 마지막 활동 시각
    /// </summary>
    public required DateTimeOffset LastActivityAt { get; init; }

    /// <summary>
    /// 세션 메타데이터 (선택적)
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// 메시지 수
    /// </summary>
    public int MessageCount { get; init; }

    /// <summary>
    /// 세션 활성화 여부
    /// </summary>
    public bool IsActive { get; init; }
}
