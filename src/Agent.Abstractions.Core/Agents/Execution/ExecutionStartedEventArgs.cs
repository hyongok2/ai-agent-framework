using System;

namespace Agent.Abstractions.Core.Agents;

/// <summary>
/// 실행 시작 이벤트 인자
/// </summary>
public sealed class ExecutionStartedEventArgs : EventArgs
{
    public required string RequestId { get; init; }
    public string? ConversationId { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}