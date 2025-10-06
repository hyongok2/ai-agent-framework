using System.Text.Json;
using AIAgentFramework.Core.Abstractions;
using AIAgentFramework.LLM.Abstractions;
using AIAgentFramework.LLM.Extensions;
using AIAgentFramework.LLM.Models;

namespace AIAgentFramework.LLM.Services.Generation;

/// <summary>
/// 콘텐츠 생성 LLM Function
/// </summary>
public class GeneratorFunction : LLMFunctionBase<GenerationInput, GenerationResult>
{
    public GeneratorFunction(
        IPromptRegistry promptRegistry,
        ILLMProvider llmProvider,
        LLMFunctionOptions? options = null,
        ILogger? logger = null)
        : base(promptRegistry, llmProvider, options, logger)
    {
    }

    public override LLMRole Role => LLMRole.Generator;

    public override string Description => "새로운 콘텐츠 생성";

    protected override string GetPromptName() => "generation";

    protected override GenerationInput ExtractInput(ILLMContext context)
    {
        return new GenerationInput
        {
            Topic = context.Get<string>("TOPIC")
                ?? throw new InvalidOperationException("TOPIC is required"),
            ContentType = context.Get<string>("CONTENT_TYPE")
                ?? throw new InvalidOperationException("CONTENT_TYPE is required"),
            Requirements = context.Get<string>("REQUIREMENTS"),
            Style = context.Get<string>("STYLE")
        };
    }

    protected override IReadOnlyDictionary<string, object> PrepareVariables(GenerationInput input)
    {
        return new Dictionary<string, object>
        {
            ["CURRENT_TIME"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            ["TOPIC"] = input.Topic,
            ["CONTENT_TYPE"] = input.ContentType,
            ["REQUIREMENTS"] = input.Requirements ?? "No specific requirements",
            ["STYLE"] = input.Style ?? "Standard"
        };
    }

    protected override ILLMResult CreateResult(string rawResponse, GenerationResult output)
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

    protected override GenerationResult ParseResponse(string response)
    {
        var cleanedResponse = CleanJsonResponse(response);

        try
        {
            var jsonDoc = JsonSerializer.Deserialize<JsonDocument>(cleanedResponse);
            var root = jsonDoc?.RootElement ?? throw new InvalidOperationException("생성 응답 파싱 실패");

            return new GenerationResult
            {
                GeneratedContent = root.GetProperty("generatedContent").GetString() ?? string.Empty,
                ContentType = root.GetProperty("contentType").GetString() ?? string.Empty,
                Length = root.GetProperty("length").GetInt32(),
                AppliedStyle = root.TryGetProperty("appliedStyle", out var styleProp)
                    ? styleProp.GetString()
                    : null,
                QualityScore = root.GetProperty("qualityScore").GetDouble()
            };
        }
        catch (Exception ex)
        {
            return new GenerationResult
            {
                GeneratedContent = "생성 실패",
                ContentType = "",
                Length = 0,
                QualityScore = 0.0,
                ErrorMessage = $"생성 응답 파싱 실패: {ex.Message}\n원본 응답: {response}"
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
