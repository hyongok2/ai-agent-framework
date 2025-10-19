using System.Text.Json;
using AIAgentFramework.Core.Abstractions;
using AIAgentFramework.LLM.Abstractions;
using AIAgentFramework.LLM.Extensions;
using AIAgentFramework.LLM.Models;
using AIAgentFramework.Tools.Abstractions;

namespace AIAgentFramework.LLM.Services.IntentAnalysis;

/// <summary>
/// 의도 분석 LLM Function
/// 사용자 입력의 의도를 파악하고 즉시 응답 가능한 경우 응답 생성
/// </summary>
public class IntentAnalyzerFunction : LLMFunctionBase<IntentAnalysisInput, IntentAnalysisResult>
{
    private readonly IToolRegistry _toolRegistry;
    private readonly ILLMRegistry _llmRegistry;

    public IntentAnalyzerFunction(
        IPromptRegistry promptRegistry,
        ILLMProvider llmProvider,
        IToolRegistry toolRegistry,
        ILLMRegistry llmRegistry,
        LLMFunctionOptions? options = null,
        ILogger? logger = null)
        : base(promptRegistry, llmProvider, options, logger)
    {
        _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
        _llmRegistry = llmRegistry ?? throw new ArgumentNullException(nameof(llmRegistry));
    }

    public override LLMRole Role => LLMRole.IntentAnalyzer;

    public override string Description => "사용자 의도 분석 및 즉시 응답 판단";

    protected override string GetPromptName() => "intent-analysis";

    protected override IntentAnalysisInput ExtractInput(ILLMContext context)
    {
        var userInput = context.UserInput;
        if (string.IsNullOrWhiteSpace(userInput))
        {
            throw new InvalidOperationException("User input is required for intent analysis");
        }

        return new IntentAnalysisInput
        {
            UserInput = userInput,
            ConversationHistory = context.Get<string>("HISTORY"),
            Context = context.Get<string>("CONTEXT")
        };
    }

    protected override IReadOnlyDictionary<string, object> PrepareVariables(IntentAnalysisInput input)
    {
        return new Dictionary<string, object>
        {
            ["USER_INPUT"] = input.UserInput,
            ["HISTORY"] = input.ConversationHistory ?? string.Empty,
            ["CONTEXT"] = input.Context ?? string.Empty,
            ["TOOLS"] = _toolRegistry.GetToolDescriptionsForLLM(),
            ["LLM_FUNCTIONS"] = _llmRegistry.GetFunctionDescriptionsForLLM()
        };
    }

    protected override ILLMResult CreateResult(string rawResponse, IntentAnalysisResult output)
    {
        return new LLMResult
        {
            Role = Role,
            RawResponse = rawResponse,
            ParsedData = output,
            IsSuccess = true,
            ErrorMessage = null
        };
    }

    protected override IntentAnalysisResult ParseResponse(string response)
    {
        var cleanedResponse = CleanJsonResponse(response);

        try
        {
            var jsonDoc = JsonSerializer.Deserialize<JsonDocument>(cleanedResponse);
            var root = jsonDoc?.RootElement ?? throw new InvalidOperationException("의도 분석 응답 파싱 실패");

            var intentTypeStr = root.GetProperty("intentType").GetString() ?? "Task";
            var intentType = Enum.Parse<IntentType>(intentTypeStr, ignoreCase: true);

            return new IntentAnalysisResult
            {
                IntentType = intentType,
                NeedsPlanning = root.GetProperty("needsPlanning").GetBoolean(),
                DirectResponse = root.TryGetProperty("directResponse", out var dirResp)
                    ? dirResp.GetString()
                    : null,
                TaskDescription = root.TryGetProperty("taskDescription", out var taskDesc)
                    ? taskDesc.GetString()
                    : null,
                Confidence = root.GetProperty("confidence").GetDouble()
            };
        }
        catch
        {
            // 파싱 실패 시 기본값으로 Task 처리 (안전한 폴백)
            return new IntentAnalysisResult
            {
                IntentType = IntentType.Task,
                NeedsPlanning = true,
                DirectResponse = null,
                TaskDescription = "의도 파악 실패 - 기본 작업 모드로 처리",
                Confidence = 0.5
            };
        }
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
