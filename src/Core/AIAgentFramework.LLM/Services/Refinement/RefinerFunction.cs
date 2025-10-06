using System.Text.Json;
using AIAgentFramework.Core.Abstractions;
using AIAgentFramework.LLM.Abstractions;
using AIAgentFramework.LLM.Extensions;
using AIAgentFramework.LLM.Models;

namespace AIAgentFramework.LLM.Services.Refinement;

public class RefinerFunction : LLMFunctionBase<RefinementInput, RefinementResult>
{
    public RefinerFunction(IPromptRegistry promptRegistry, ILLMProvider llmProvider, LLMFunctionOptions? options = null, ILogger? logger = null)
        : base(promptRegistry, llmProvider, options, logger) { }

    public override LLMRole Role => LLMRole.Refiner;
    public override string Description => "콘텐츠 개선 및 재작성";
    protected override string GetPromptName() => "refinement";

    protected override RefinementInput ExtractInput(ILLMContext context)
    {
        return new RefinementInput
        {
            OriginalContent = context.Get<string>("ORIGINAL_CONTENT") ?? throw new InvalidOperationException("ORIGINAL_CONTENT is required"),
            Purpose = context.Get<string>("PURPOSE") ?? throw new InvalidOperationException("PURPOSE is required"),
            Feedback = context.Get<string>("FEEDBACK")
        };
    }

    protected override IReadOnlyDictionary<string, object> PrepareVariables(RefinementInput input)
    {
        return new Dictionary<string, object>
        {
            ["CURRENT_TIME"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            ["ORIGINAL_CONTENT"] = input.OriginalContent,
            ["PURPOSE"] = input.Purpose,
            ["FEEDBACK"] = input.Feedback ?? "No specific feedback"
        };
    }

    protected override ILLMResult CreateResult(string rawResponse, RefinementResult output)
    {
        return new LLMResult { Role = Role, RawResponse = rawResponse, ParsedData = output, IsSuccess = string.IsNullOrEmpty(output.ErrorMessage), ErrorMessage = output.ErrorMessage };
    }

    protected override RefinementResult ParseResponse(string response)
    {
        var cleanedResponse = CleanJsonResponse(response);
        try
        {
            var jsonDoc = JsonSerializer.Deserialize<JsonDocument>(cleanedResponse);
            var root = jsonDoc?.RootElement ?? throw new InvalidOperationException("개선 응답 파싱 실패");
            return new RefinementResult
            {
                RefinedContent = root.GetProperty("refinedContent").GetString() ?? string.Empty,
                Changes = root.GetProperty("changes").EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToList(),
                ImprovementScore = root.GetProperty("improvementScore").GetDouble()
            };
        }
        catch (Exception ex)
        {
            return new RefinementResult { RefinedContent = "개선 실패", Changes = new List<string>(), ImprovementScore = 0.0, ErrorMessage = $"개선 응답 파싱 실패: {ex.Message}\n원본 응답: {response}" };
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
