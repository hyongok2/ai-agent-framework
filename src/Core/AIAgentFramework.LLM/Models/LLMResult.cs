using AIAgentFramework.LLM.Abstractions;

namespace AIAgentFramework.LLM.Models;

/// <summary>
/// LLM 실행 결과 구현체
/// </summary>
public class LLMResult : ILLMResult
{
    public LLMRole Role { get; init; }
    public string RawResponse { get; init; } = string.Empty;
    public object? ParsedData { get; init; }
    public string? NextAction { get; init; }
    public int TokenUsage { get; init; }
    public bool IsSuccess { get; init; } = true;
    public string? ErrorMessage { get; init; }
    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CompletedAt { get; init; } = DateTimeOffset.UtcNow;
}
