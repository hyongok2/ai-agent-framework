using AIAgentFramework.Core.Abstractions;

namespace AIAgentFramework.Core.Models;

/// <summary>
/// 로그 엔트리 구현체
/// </summary>
public class LogEntry : ILogEntry
{
    public required string LogType { get; init; }
    public required string TargetName { get; init; }
    public required string ExecutionId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public object? Request { get; init; }
    public object? Response { get; init; }
    public required long DurationMs { get; init; }
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }

    public static LogEntry Create(
        string logType,
        string targetName,
        string executionId,
        DateTimeOffset timestamp,
        object? request,
        object? response,
        long durationMs,
        bool success,
        string? errorMessage = null)
    {
        return new LogEntry
        {
            LogType = logType,
            TargetName = targetName,
            ExecutionId = executionId,
            Timestamp = timestamp,
            Request = request,
            Response = response,
            DurationMs = durationMs,
            Success = success,
            ErrorMessage = errorMessage
        };
    }
}
