using System.Text.Json;
using AIAgentFramework.Core.Abstractions;
using AIAgentFramework.Execution.Abstractions;
using AIAgentFramework.Execution.Models;
using AIAgentFramework.LLM.Services.Planning;

namespace AIAgentFramework.Execution.Services;

/// <summary>
/// Tool 실행 담당
/// </summary>
public class ToolStepExecutor : IStepExecutor
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = false };

    public async Task<StepExecutionResult> ExecuteAsync(
        TaskStep step,
        ExecutableItem executable,
        string? parameters,
        string userRequest,
        IAgentContext agentContext,
        Action<string>? onStreamChunk = null,
        CancellationToken cancellationToken = default)
    {
        if (executable.Type != ExecutableType.Tool || executable.Tool == null)
        {
            throw new ArgumentException("ExecutableItem must be a Tool", nameof(executable));
        }

        var tool = executable.Tool;
        var toolContext = Core.Models.ExecutionContext.Create();
        object? toolInput = parameters;

        // JSON 문자열이면 파싱
        if (!string.IsNullOrEmpty(parameters))
        {
            try
            {
                toolInput = JsonSerializer.Deserialize<JsonElement>(parameters);
            }
            catch
            {
                // JSON 파싱 실패하면 문자열 그대로 사용
                toolInput = parameters;
            }
        }

        var toolResult = await tool.ExecuteAsync(toolInput, toolContext, cancellationToken);

        // Output을 JSON 직렬화
        string? output = null;
        if (toolResult.Data != null)
        {
            if (toolResult.Data is string str)
            {
                output = str;
            }
            else
            {
                output = JsonSerializer.Serialize(toolResult.Data, _jsonOptions);
            }
        }

        return new StepExecutionResult
        {
            StepNumber = step.StepNumber,
            Description = step.Description,
            ToolName = step.ToolName,
            IsSuccess = toolResult.IsSuccess,
            Output = output,
            ErrorMessage = toolResult.ErrorMessage
        };
    }
}
