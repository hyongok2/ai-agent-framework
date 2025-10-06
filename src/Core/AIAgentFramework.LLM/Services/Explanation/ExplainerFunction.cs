using System.Text.Json;
using AIAgentFramework.Core.Abstractions;
using AIAgentFramework.LLM.Abstractions;
using AIAgentFramework.LLM.Extensions;
using AIAgentFramework.LLM.Models;

namespace AIAgentFramework.LLM.Services.Explanation;

public class ExplainerFunction : LLMFunctionBase<ExplanationInput, ExplanationResult>
{
    public ExplainerFunction(IPromptRegistry promptRegistry, ILLMProvider llmProvider, LLMFunctionOptions? options = null, ILogger? logger = null)
        : base(promptRegistry, llmProvider, options, logger) { }

    public override LLMRole Role => LLMRole.Explainer;
    public override string Description => "개념 설명 및 교육";
    protected override string GetPromptName() => "explanation";

    protected override ExplanationInput ExtractInput(ILLMContext context)
    {
        return new ExplanationInput
        {
            Topic = context.Get<string>("TOPIC") ?? throw new InvalidOperationException("TOPIC is required"),
            AudienceLevel = context.Get<string>("AUDIENCE_LEVEL"),
            Focus = context.Get<string>("FOCUS")
        };
    }

    protected override IReadOnlyDictionary<string, object> PrepareVariables(ExplanationInput input)
    {
        return new Dictionary<string, object>
        {
            ["CURRENT_TIME"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            ["TOPIC"] = input.Topic,
            ["AUDIENCE_LEVEL"] = input.AudienceLevel ?? "General",
            ["FOCUS"] = input.Focus ?? "Comprehensive explanation"
        };
    }

    protected override ILLMResult CreateResult(string rawResponse, ExplanationResult output)
    {
        return new LLMResult { Role = Role, RawResponse = rawResponse, ParsedData = output, IsSuccess = string.IsNullOrEmpty(output.ErrorMessage), ErrorMessage = output.ErrorMessage };
    }

    protected override ExplanationResult ParseResponse(string response)
    {
        var cleanedResponse = CleanJsonResponse(response);
        try
        {
            var jsonDoc = JsonSerializer.Deserialize<JsonDocument>(cleanedResponse);
            var root = jsonDoc?.RootElement ?? throw new InvalidOperationException("설명 응답 파싱 실패");
            return new ExplanationResult
            {
                Explanation = root.GetProperty("explanation").GetString() ?? string.Empty,
                KeyPoints = root.GetProperty("keyPoints").EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToList(),
                Examples = root.TryGetProperty("examples", out var exProp) ? exProp.EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToList() : null
            };
        }
        catch (Exception ex)
        {
            return new ExplanationResult { Explanation = "설명 실패", KeyPoints = new List<string>(), ErrorMessage = $"설명 응답 파싱 실패: {ex.Message}\n원본 응답: {response}" };
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
