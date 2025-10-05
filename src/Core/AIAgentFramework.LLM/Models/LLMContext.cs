using AIAgentFramework.LLM.Abstractions;

namespace AIAgentFramework.LLM.Models;

/// <summary>
/// LLM 실행 컨텍스트 구현체
/// </summary>
public class LLMContext : ILLMContext
{
    public string UserInput { get; init; } = string.Empty;

    public IReadOnlyList<string> ConversationHistory { get; init; } = Array.Empty<string>();

    public IReadOnlyDictionary<string, object> Parameters { get; init; } = new Dictionary<string, object>();

    public IReadOnlyDictionary<string, string> SystemInfo { get; init; } = new Dictionary<string, string>();

    public string ExecutionId { get; init; } = Guid.NewGuid().ToString();

    public string? UserId { get; init; }

    public string SessionId { get; init; } = Guid.NewGuid().ToString();

    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    IReadOnlyDictionary<string, object> AIAgentFramework.Core.Abstractions.IExecutionContext.Metadata => new Dictionary<string, object>();
}
