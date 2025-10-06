using System.Text.Json;
using AIAgentFramework.Core.Abstractions;
using AIAgentFramework.LLM.Abstractions;
using AIAgentFramework.LLM.Extensions;
using AIAgentFramework.LLM.Models;

namespace AIAgentFramework.LLM.Services.Reasoning;

public class ReasonerFunction : LLMFunctionBase<ReasoningInput, ReasoningResult>
{
    public ReasonerFunction(IPromptRegistry promptRegistry, ILLMProvider llmProvider, LLMFunctionOptions? options = null, ILogger? logger = null)
        : base(promptRegistry, llmProvider, options, logger) { }

    public override LLMRole Role => LLMRole.Reasoner;
    public override string Description => "논리적 추론 및 결론 도출";
    protected override string GetPromptName() => "reasoning";

    protected override ReasoningInput ExtractInput(ILLMContext context)
    {
        return new ReasoningInput
        {
            Problem = context.Get<string>("PROBLEM") ?? throw new InvalidOperationException("PROBLEM is required"),
            Facts = context.Get<string>("FACTS"),
            Rules = context.Get<string>("RULES")
        };
    }

    protected override IReadOnlyDictionary<string, object> PrepareVariables(ReasoningInput input)
    {
        return new Dictionary<string, object>
        {
            ["CURRENT_TIME"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            ["PROBLEM"] = input.Problem,
            ["FACTS"] = input.Facts ?? "No specific facts provided",
            ["RULES"] = input.Rules ?? "Standard logical rules"
        };
    }

    protected override ILLMResult CreateResult(string rawResponse, ReasoningResult output)
    {
        return new LLMResult { Role = Role, RawResponse = rawResponse, ParsedData = output, IsSuccess = string.IsNullOrEmpty(output.ErrorMessage), ErrorMessage = output.ErrorMessage };
    }

    protected override ReasoningResult ParseResponse(string response)
    {
        var cleanedResponse = CleanJsonResponse(response);
        try
        {
            var jsonDoc = JsonSerializer.Deserialize<JsonDocument>(cleanedResponse);
            var root = jsonDoc?.RootElement ?? throw new InvalidOperationException("추론 응답 파싱 실패");
            return new ReasoningResult
            {
                Conclusion = root.GetProperty("conclusion").GetString() ?? string.Empty,
                ReasoningSteps = root.GetProperty("reasoningSteps").EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToList(),
                Confidence = root.GetProperty("confidence").GetDouble(),
                Assumptions = root.TryGetProperty("assumptions", out var assProp) ? assProp.EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToList() : null
            };
        }
        catch (Exception ex)
        {
            return new ReasoningResult { Conclusion = "추론 실패", ReasoningSteps = new List<string>(), Confidence = 0.0, ErrorMessage = $"추론 응답 파싱 실패: {ex.Message}\n원본 응답: {response}" };
        }
    }

    private string CleanJsonResponse(string response)
    {
        var trimmed = response.Trim();
        if (trimmed.StartsWith("```json")) trimmed = trimmed.Substring("```json".Length);
        else if (trimmed.StartsWith("```")) trimmed = trimmed.Substring("```".Length);
        if (trimmed.EndsWith("```")) trimmed = trimmed.Substring(0, trimmed.Length - 3);
        return trimmed.Trim();
    }
}
