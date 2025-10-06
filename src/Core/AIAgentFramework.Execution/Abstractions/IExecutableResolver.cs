using AIAgentFramework.LLM.Abstractions;
using AIAgentFramework.Tools.Abstractions;

namespace AIAgentFramework.Execution.Abstractions;

/// <summary>
/// Tool 또는 LLM Function을 찾는 Resolver
/// </summary>
public interface IExecutableResolver
{
    /// <summary>
    /// Tool 이름으로 실행 가능한 객체(Tool 또는 LLM Function) 찾기
    /// </summary>
    ExecutableItem? Resolve(string toolName);
}

/// <summary>
/// 실행 가능한 항목 (Tool 또는 LLM Function)
/// </summary>
public record ExecutableItem
{
    public ExecutableType Type { get; init; }
    public ITool? Tool { get; init; }
    public ILLMFunction? LLMFunction { get; init; }
}

/// <summary>
/// 실행 가능한 항목의 타입
/// </summary>
public enum ExecutableType
{
    Tool,
    LLMFunction
}
