namespace Agent.Core.Abstractions.Streaming.Chunks;

/// <summary>
/// 상태 타입
/// </summary>
public enum StatusType
{
    Initializing,
    Started,
    InProgress,
    WaitingForInput,
    Processing,
    Completed,
    Failed,
    Cancelled,
    Timeout
}