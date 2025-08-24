using System;

namespace Agent.Abstractions.Core.Agents;

/// <summary>
/// 실행 완료 이벤트 인자
/// </summary>
public sealed class ExecutionCompletedEventArgs : EventArgs
{
    public required string RequestId { get; init; }
    public string? ConversationId { get; init; }
    public bool Success { get; init; }
    public TimeSpan Duration { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
