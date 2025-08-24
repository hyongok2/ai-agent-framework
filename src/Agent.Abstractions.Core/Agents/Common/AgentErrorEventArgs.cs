using System;

namespace Agent.Abstractions.Core.Agents;

/// <summary>
/// 에러 이벤트 인자
/// </summary>
public sealed class AgentErrorEventArgs : EventArgs
{
    public required Exception Exception { get; init; }
    public string? RequestId { get; init; }
    public string? ConversationId { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
