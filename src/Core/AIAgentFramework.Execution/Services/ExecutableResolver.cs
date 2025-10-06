using AIAgentFramework.Execution.Abstractions;
using AIAgentFramework.LLM.Abstractions;
using AIAgentFramework.LLM.Models;
using AIAgentFramework.Tools.Models;

namespace AIAgentFramework.Execution.Services;

/// <summary>
/// Tool 또는 LLM Function을 찾는 Resolver 구현체
/// </summary>
public class ExecutableResolver : IExecutableResolver
{
    private readonly ToolRegistry _toolRegistry;
    private readonly ILLMRegistry _llmRegistry;

    public ExecutableResolver(ToolRegistry toolRegistry, ILLMRegistry llmRegistry)
    {
        _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
        _llmRegistry = llmRegistry ?? throw new ArgumentNullException(nameof(llmRegistry));
    }

    public ExecutableItem? Resolve(string toolName)
    {
        // 1. Tool 찾기
        var tool = _toolRegistry.GetTool(toolName);
        if (tool != null)
        {
            return new ExecutableItem
            {
                Type = ExecutableType.Tool,
                Tool = tool,
                LLMFunction = null
            };
        }

        // 2. LLM Function 찾기
        if (Enum.TryParse<LLMRole>(toolName, out var role))
        {
            var llmFunction = _llmRegistry.GetFunction(role);
            if (llmFunction != null)
            {
                return new ExecutableItem
                {
                    Type = ExecutableType.LLMFunction,
                    Tool = null,
                    LLMFunction = llmFunction
                };
            }
        }

        return null;
    }
}
