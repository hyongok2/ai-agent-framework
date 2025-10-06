using System.Collections.Concurrent;
using AIAgentFramework.Core.Abstractions;
using AIAgentFramework.Core.Models;

namespace AIAgentFramework.Core.Services.ChatHistory;

/// <summary>
/// 인메모리 채팅 히스토리 저장소
/// 스레드 안전, 휘발성
/// </summary>
public class InMemoryChatHistoryStore : IChatHistoryStore
{
    private readonly ConcurrentDictionary<string, ChatSession> _sessions = new();
    private readonly ConcurrentDictionary<string, List<ChatMessage>> _messages = new();
    private readonly object _lock = new();

    public Task<ChatSession> CreateSessionAsync(string? userId = null, string? title = null, CancellationToken cancellationToken = default)
    {
        var sessionId = Guid.NewGuid().ToString("N");
        var now = DateTimeOffset.UtcNow;

        var session = new ChatSession
        {
            SessionId = sessionId,
            UserId = userId,
            Title = title ?? $"Session {sessionId[..8]}",
            CreatedAt = now,
            LastActivityAt = now,
            MessageCount = 0,
            IsActive = true
        };

        _sessions.TryAdd(sessionId, session);
        _messages.TryAdd(sessionId, new List<ChatMessage>());

        return Task.FromResult(session);
    }

    public Task<ChatSession?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        _sessions.TryGetValue(sessionId, out var session);
        return Task.FromResult(session);
    }

    public Task<List<ChatSession>> GetSessionsAsync(string? userId = null, CancellationToken cancellationToken = default)
    {
        var sessions = _sessions.Values
            .Where(s => userId == null || s.UserId == userId)
            .OrderByDescending(s => s.LastActivityAt)
            .ToList();

        return Task.FromResult(sessions);
    }

    public Task<bool> DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var removed = _sessions.TryRemove(sessionId, out _);
        if (removed)
        {
            _messages.TryRemove(sessionId, out _);
        }
        return Task.FromResult(removed);
    }

    public Task<ChatMessage> AddMessageAsync(
        string sessionId,
        MessageRole role,
        string content,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            throw new InvalidOperationException($"Session not found: {sessionId}");
        }

        var messageId = Guid.NewGuid().ToString("N");
        var now = DateTimeOffset.UtcNow;

        var message = new ChatMessage
        {
            MessageId = messageId,
            SessionId = sessionId,
            Role = role,
            Content = content,
            CreatedAt = now,
            Metadata = metadata
        };

        lock (_lock)
        {
            if (_messages.TryGetValue(sessionId, out var messages))
            {
                messages.Add(message);

                // 세션 업데이트
                var updatedSession = new ChatSession
                {
                    SessionId = session.SessionId,
                    UserId = session.UserId,
                    Title = session.Title,
                    CreatedAt = session.CreatedAt,
                    LastActivityAt = now,
                    Metadata = session.Metadata,
                    MessageCount = messages.Count,
                    IsActive = session.IsActive
                };
                _sessions.TryUpdate(sessionId, updatedSession, session);
            }
        }

        return Task.FromResult(message);
    }

    public Task<List<ChatMessage>> GetMessagesAsync(string sessionId, int? limit = null, CancellationToken cancellationToken = default)
    {
        if (!_messages.TryGetValue(sessionId, out var messages))
        {
            return Task.FromResult(new List<ChatMessage>());
        }

        lock (_lock)
        {
            var result = limit.HasValue
                ? messages.TakeLast(limit.Value).ToList()
                : messages.ToList();

            return Task.FromResult(result);
        }
    }

    public Task<List<ChatMessage>> GetRecentMessagesAsync(string sessionId, int count, CancellationToken cancellationToken = default)
    {
        return GetMessagesAsync(sessionId, count, cancellationToken);
    }

    public Task<bool> ClearMessagesAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            return Task.FromResult(false);
        }

        lock (_lock)
        {
            if (_messages.TryGetValue(sessionId, out var messages))
            {
                messages.Clear();

                // 세션 업데이트
                var updatedSession = new ChatSession
                {
                    SessionId = session.SessionId,
                    UserId = session.UserId,
                    Title = session.Title,
                    CreatedAt = session.CreatedAt,
                    LastActivityAt = session.LastActivityAt,
                    Metadata = session.Metadata,
                    MessageCount = 0,
                    IsActive = session.IsActive
                };
                _sessions.TryUpdate(sessionId, updatedSession, session);

                return Task.FromResult(true);
            }
        }

        return Task.FromResult(false);
    }

    public Task<bool> UpdateSessionMetadataAsync(string sessionId, Dictionary<string, object> metadata, CancellationToken cancellationToken = default)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            return Task.FromResult(false);
        }

        var updatedSession = new ChatSession
        {
            SessionId = session.SessionId,
            UserId = session.UserId,
            Title = session.Title,
            CreatedAt = session.CreatedAt,
            LastActivityAt = DateTimeOffset.UtcNow,
            Metadata = metadata,
            MessageCount = session.MessageCount,
            IsActive = session.IsActive
        };

        return Task.FromResult(_sessions.TryUpdate(sessionId, updatedSession, session));
    }
}
