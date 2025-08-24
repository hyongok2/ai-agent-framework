using System;

namespace Agent.Abstractions.Core.Agents;

/// <summary>
/// 상태 변경 이벤트 인자
/// </summary>
public sealed class AgentStatusChangedEventArgs : EventArgs
{
    public AgentStatus OldStatus { get; init; }
    public AgentStatus NewStatus { get; init; }
    public string? Reason { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}