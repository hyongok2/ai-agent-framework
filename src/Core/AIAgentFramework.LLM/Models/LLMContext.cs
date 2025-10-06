using AIAgentFramework.Core.Abstractions;
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

    /// <summary>
    /// AgentContext로부터 LLMContext 생성 (글로벌 컨텍스트 기반)
    /// </summary>
    public static LLMContext FromAgentContext(IAgentContext agentContext, string userInput)
    {
        return new LLMContext
        {
            UserInput = userInput,
            Parameters = new Dictionary<string, object>(agentContext.Variables),
            ExecutionId = agentContext.Get<string>("ExecutionId") ?? Guid.NewGuid().ToString(),
            UserId = agentContext.Get<string>("UserId"),
            SessionId = agentContext.Get<string>("SessionId") ?? Guid.NewGuid().ToString()
        };
    }
}
