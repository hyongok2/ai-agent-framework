using System.Text.Json;
using AIAgentFramework.Core.Abstractions;
using AIAgentFramework.LLM.Abstractions;
using AIAgentFramework.LLM.Extensions;
using AIAgentFramework.LLM.Models;

namespace AIAgentFramework.LLM.Services.Classification;

public class ClassifierFunction : LLMFunctionBase<ClassificationInput, ClassificationResult>
{
    public ClassifierFunction(
        IPromptRegistry promptRegistry,
        ILLMProvider llmProvider,
        LLMFunctionOptions? options = null,
        ILogger? logger = null)
        : base(promptRegistry, llmProvider, options, logger)
    {
    }

    public override LLMRole Role => LLMRole.Classifier;
    public override string Description => "텍스트 분류 및 카테고리화";
    protected override string GetPromptName() => "classification";

    protected override ClassificationInput ExtractInput(ILLMContext context)
    {
        return new ClassificationInput
        {
            Content = context.Get<string>("CONTENT") ?? throw new InvalidOperationException("CONTENT is required"),
            Categories = context.Get<List<string>>("CATEGORIES") ?? throw new InvalidOperationException("CATEGORIES is required"),
            Context = context.Get<string>("CONTEXT")
        };
    }

    protected override IReadOnlyDictionary<string, object> PrepareVariables(ClassificationInput input)
    {
        return new Dictionary<string, object>
        {
            ["CURRENT_TIME"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            ["CONTENT"] = input.Content,
            ["CATEGORIES"] = string.Join(", ", input.Categories),
            ["CONTEXT"] = input.Context ?? "General classification"
        };
    }

    protected override ILLMResult CreateResult(string rawResponse, ClassificationResult output)
    {
        return new LLMResult
        {
            Role = Role,
            RawResponse = rawResponse,
            ParsedData = output,
            IsSuccess = string.IsNullOrEmpty(output.ErrorMessage),
            ErrorMessage = output.ErrorMessage
        };
    }

    protected override ClassificationResult ParseResponse(string response)
    {
        var cleanedResponse = CleanJsonResponse(response);
        try
        {
            var jsonDoc = JsonSerializer.Deserialize<JsonDocument>(cleanedResponse);
            var root = jsonDoc?.RootElement ?? throw new InvalidOperationException("분류 응답 파싱 실패");

            return new ClassificationResult
            {
                PrimaryCategory = root.GetProperty("primaryCategory").GetString() ?? string.Empty,
                Confidence = root.GetProperty("confidence").GetDouble(),
                AlternativeCategories = root.TryGetProperty("alternativeCategories", out var altProp)
                    ? altProp.EnumerateArray().Select(x => new CategoryScore
                    {
                        Category = x.GetProperty("category").GetString() ?? string.Empty,
                        Score = x.GetProperty("score").GetDouble()
                    }).ToList()
                    : null,
                Reasoning = root.TryGetProperty("reasoning", out var reasonProp) ? reasonProp.GetString() : null
            };
        }
        catch (Exception ex)
        {
            return new ClassificationResult
            {
                PrimaryCategory = "분류 실패",
                Confidence = 0.0,
                ErrorMessage = $"분류 응답 파싱 실패: {ex.Message}\n원본 응답: {response}"
            };
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
