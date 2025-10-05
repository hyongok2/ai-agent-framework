using System.Text.Json;
using AIAgentFramework.LLM.Abstractions;
using AIAgentFramework.LLM.Extensions;
using AIAgentFramework.LLM.Models;
using AIAgentFramework.Tools.Abstractions;

namespace AIAgentFramework.LLM.Services.ToolSelection;

/// <summary>
/// 사용자 요청에 적합한 Tool을 선택하는 LLM 기능
/// </summary>
public class ToolSelectorFunction : LLMFunctionBase<ToolSelectionInput, ToolSelectionResult>
{
    private readonly IToolRegistry _toolRegistry;

    public ToolSelectorFunction(
        IPromptRegistry promptRegistry,
        ILLMProvider llmProvider,
        IToolRegistry toolRegistry,
        LLMFunctionOptions? options = null)
        : base(promptRegistry, llmProvider, options)
    {
        _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
    }

    public override LLMRole Role => LLMRole.Planner;

    public override string Description => "사용자 요청에 맞는 Tool 선택";

    protected override string GetPromptName() => "tool-selection";

    protected override ToolSelectionInput ExtractInput(ILLMContext context)
    {
        return new ToolSelectionInput
        {
            UserRequest = context.UserInput,
            Context = context.Get<string>("CONTEXT"),
            History = context.Get<string>("HISTORY")
        };
    }

    protected override IReadOnlyDictionary<string, object> PrepareVariables(ToolSelectionInput input)
    {
        var variables = new Dictionary<string, object>
        {
            ["TOOLS"] = _toolRegistry.GetToolDescriptionsForLLM(),
            ["USER_INPUT"] = input.UserRequest
        };

        if (!string.IsNullOrWhiteSpace(input.Context))
        {
            variables["CONTEXT"] = input.Context;
        }

        if (!string.IsNullOrWhiteSpace(input.History))
        {
            variables["HISTORY"] = input.History;
        }

        return variables;
    }

    protected override ToolSelectionResult ParseResponse(string response)
    {
        var cleanedResponse = CleanJsonResponse(response);

        var jsonDoc = JsonSerializer.Deserialize<JsonDocument>(cleanedResponse);
        var root = jsonDoc?.RootElement ?? throw new InvalidOperationException("응답 파싱 실패");

        return new ToolSelectionResult
        {
            ToolName = root.GetProperty("toolName").GetString() ?? string.Empty,
            Parameters = root.GetProperty("parameters").GetString() ?? string.Empty
        };
    }

    protected override ILLMResult CreateResult(string rawResponse, ToolSelectionResult output)
    {
        return new LLMResult
        {
            Role = Role,
            RawResponse = rawResponse,
            ParsedData = output,
            IsSuccess = true
        };
    }

    private string CleanJsonResponse(string response)
    {
        var trimmed = response.Trim();

        if (trimmed.StartsWith("```json"))
        {
            trimmed = trimmed.Substring("```json".Length);
        }
        else if (trimmed.StartsWith("```"))
        {
            trimmed = trimmed.Substring("```".Length);
        }

        if (trimmed.EndsWith("```"))
        {
            trimmed = trimmed.Substring(0, trimmed.Length - 3);
        }

        return trimmed.Trim();
    }
}
