using AIAgentFramework.Core.Models;

namespace AIAgentFramework.Core.Abstractions;

/// <summary>
/// 채팅 히스토리 저장소 인터페이스
/// </summary>
public interface IChatHistoryStore
{
    /// <summary>
    /// 새 세션 생성
    /// </summary>
    Task<ChatSession> CreateSessionAsync(string? userId = null, string? title = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 세션 조회
    /// </summary>
    Task<ChatSession?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 모든 세션 조회
    /// </summary>
    Task<List<ChatSession>> GetSessionsAsync(string? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 세션 삭제
    /// </summary>
    Task<bool> DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 메시지 추가
    /// </summary>
    Task<ChatMessage> AddMessageAsync(string sessionId, MessageRole role, string content, Dictionary<string, object>? metadata = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 세션의 메시지 목록 조회
    /// </summary>
    Task<List<ChatMessage>> GetMessagesAsync(string sessionId, int? limit = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 최근 N개 메시지 조회
    /// </summary>
    Task<List<ChatMessage>> GetRecentMessagesAsync(string sessionId, int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// 세션의 모든 메시지 삭제
    /// </summary>
    Task<bool> ClearMessagesAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 세션 메타데이터 업데이트
    /// </summary>
    Task<bool> UpdateSessionMetadataAsync(string sessionId, Dictionary<string, object> metadata, CancellationToken cancellationToken = default);
}
