using System.Text.Json;
using AIAgentFramework.Tools.Abstractions;

namespace AIAgentFramework.Tools.Models;

/// <summary>
/// Tool 실행 결과 기본 구현
/// </summary>
public class ToolResult : IToolResult
{
    public string ToolName { get; init; } = string.Empty;
    public object? Data { get; init; }
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset CompletedAt { get; init; }

    public string ToJson()
    {
        return JsonSerializer.Serialize(new
        {
            ToolName,
            Data,
            IsSuccess,
            ErrorMessage,
            StartedAt,
            CompletedAt,
            DurationMs = (CompletedAt - StartedAt).TotalMilliseconds
        });
    }

    public static ToolResult Success(string toolName, object? data, DateTimeOffset startedAt)
    {
        return new ToolResult
        {
            ToolName = toolName,
            Data = data,
            IsSuccess = true,
            StartedAt = startedAt,
            CompletedAt = DateTimeOffset.UtcNow
        };
    }

    public static ToolResult Failure(string toolName, string errorMessage, DateTimeOffset startedAt)
    {
        return new ToolResult
        {
            ToolName = toolName,
            IsSuccess = false,
            ErrorMessage = errorMessage,
            StartedAt = startedAt,
            CompletedAt = DateTimeOffset.UtcNow
        };
    }
}
