using System.Text.Json;
using AIAgentFramework.Core.Abstractions;
using AIAgentFramework.LLM.Abstractions;
using AIAgentFramework.LLM.Extensions;
using AIAgentFramework.LLM.Models;

namespace AIAgentFramework.LLM.Services.ParameterGeneration;

/// <summary>
/// Tool 실행에 필요한 정확한 파라미터를 생성하는 LLM 기능
/// </summary>
public class ParameterGeneratorFunction : LLMFunctionBase<ParameterGenerationInput, ParameterGenerationResult>
{
    public ParameterGeneratorFunction(
        IPromptRegistry promptRegistry,
        ILLMProvider llmProvider,
        LLMFunctionOptions? options = null,
        ILogger? logger = null)
        : base(promptRegistry, llmProvider, options, logger)
    {
    }

    public override LLMRole Role => LLMRole.ToolParameterSetter;

    public override string Description => "Tool 실행에 필요한 정확한 파라미터 생성";

    protected override string GetPromptName() => "parameter-generation";

    protected override ParameterGenerationInput ExtractInput(ILLMContext context)
    {
        return new ParameterGenerationInput
        {
            UserRequest = context.UserInput,
            ToolName = context.Get<string>("TOOL_NAME") ?? throw new InvalidOperationException("TOOL_NAME is required"),
            ToolInputSchema = context.Get<string>("TOOL_INPUT_SCHEMA") ?? throw new InvalidOperationException("TOOL_INPUT_SCHEMA is required"),
            StepDescription = context.Get<string>("STEP_DESCRIPTION") ?? throw new InvalidOperationException("STEP_DESCRIPTION is required"),
            PreviousResults = context.Get<string>("PREVIOUS_RESULTS"),
            AdditionalContext = context.Get<string>("ADDITIONAL_CONTEXT")
        };
    }

    protected override IReadOnlyDictionary<string, object> PrepareVariables(ParameterGenerationInput input)
    {
        var variables = new Dictionary<string, object>
        {
            ["USER_REQUEST"] = input.UserRequest,
            ["TOOL_NAME"] = input.ToolName,
            ["TOOL_INPUT_SCHEMA"] = input.ToolInputSchema,
            ["STEP_DESCRIPTION"] = input.StepDescription
        };

        if (!string.IsNullOrWhiteSpace(input.PreviousResults))
        {
            variables["PREVIOUS_RESULTS"] = input.PreviousResults;
        }

        if (!string.IsNullOrWhiteSpace(input.AdditionalContext))
        {
            variables["ADDITIONAL_CONTEXT"] = input.AdditionalContext;
        }

        return variables;
    }

    protected override ParameterGenerationResult ParseResponse(string response)
    {
        var cleanedResponse = CleanJsonResponse(response);

        try
        {
            var jsonDoc = JsonSerializer.Deserialize<JsonDocument>(cleanedResponse);
            var root = jsonDoc?.RootElement ?? throw new InvalidOperationException("응답 파싱 실패");

            return new ParameterGenerationResult
            {
                ToolName = root.GetProperty("toolName").GetString() ?? string.Empty,
                Parameters = root.GetProperty("parameters").GetString() ?? string.Empty,
                Reasoning = root.TryGetProperty("reasoning", out var reasoningProp)
                    ? reasoningProp.GetString()
                    : null,
                IsValid = root.TryGetProperty("isValid", out var isValidProp)
                    ? isValidProp.GetBoolean()
                    : true,
                ErrorMessage = root.TryGetProperty("errorMessage", out var errorProp)
                    ? errorProp.GetString()
                    : null
            };
        }
        catch (Exception ex)
        {
            // 파싱 실패 시 실패 결과 반환
            return new ParameterGenerationResult
            {
                ToolName = string.Empty,
                Parameters = string.Empty,
                Reasoning = null,
                IsValid = false,
                ErrorMessage = $"파라미터 생성 응답 파싱 실패: {ex.Message}"
            };
        }
    }

    protected override ILLMResult CreateResult(string rawResponse, ParameterGenerationResult output)
    {
        return new LLMResult
        {
            Role = Role,
            RawResponse = rawResponse,
            ParsedData = output,
            IsSuccess = output.IsValid,
            ErrorMessage = output.ErrorMessage
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
