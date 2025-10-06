using System.Text.Json;
using AIAgentFramework.Core.Abstractions;
using AIAgentFramework.LLM.Abstractions;
using AIAgentFramework.LLM.Extensions;
using AIAgentFramework.LLM.Models;

namespace AIAgentFramework.LLM.Services.Analysis;

/// <summary>
/// 입력 분석 및 해석 LLM Function
/// </summary>
public class AnalyzerFunction : LLMFunctionBase<AnalysisInput, AnalysisResult>
{
    public AnalyzerFunction(
        IPromptRegistry promptRegistry,
        ILLMProvider llmProvider,
        LLMFunctionOptions? options = null,
        ILogger? logger = null)
        : base(promptRegistry, llmProvider, options, logger)
    {
    }

    public override LLMRole Role => LLMRole.Analyzer;

    public override string Description => "입력 텍스트 분석 및 해석";

    protected override string GetPromptName() => "analysis";

    protected override AnalysisInput ExtractInput(ILLMContext context)
    {
        return new AnalysisInput
        {
            Content = context.Get<string>("CONTENT")
                ?? throw new InvalidOperationException("CONTENT is required"),
            Purpose = context.Get<string>("PURPOSE"),
            FocusArea = context.Get<string>("FOCUS_AREA")
        };
    }

    protected override IReadOnlyDictionary<string, object> PrepareVariables(AnalysisInput input)
    {
        return new Dictionary<string, object>
        {
            ["CURRENT_TIME"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            ["CONTENT"] = input.Content,
            ["PURPOSE"] = input.Purpose ?? "General analysis",
            ["FOCUS_AREA"] = input.FocusArea ?? "intent, entities, sentiment"
        };
    }

    protected override ILLMResult CreateResult(string rawResponse, AnalysisResult output)
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

    protected override AnalysisResult ParseResponse(string response)
    {
        var cleanedResponse = CleanJsonResponse(response);

        try
        {
            var jsonDoc = JsonSerializer.Deserialize<JsonDocument>(cleanedResponse);
            var root = jsonDoc?.RootElement ?? throw new InvalidOperationException("분석 응답 파싱 실패");

            return new AnalysisResult
            {
                Intent = root.GetProperty("intent").GetString() ?? string.Empty,
                Entities = root.GetProperty("entities").EnumerateArray()
                    .Select(x => x.GetString() ?? string.Empty)
                    .Where(x => !string.IsNullOrEmpty(x))
                    .ToList(),
                Sentiment = root.TryGetProperty("sentiment", out var sentimentProp)
                    ? sentimentProp.GetString()
                    : null,
                Confidence = root.GetProperty("confidence").GetDouble(),
                DetailedAnalysis = root.TryGetProperty("detailedAnalysis", out var analysisProp)
                    ? analysisProp.GetString()
                    : null,
                Ambiguities = root.TryGetProperty("ambiguities", out var ambProp)
                    ? ambProp.EnumerateArray()
                        .Select(x => x.GetString() ?? string.Empty)
                        .Where(x => !string.IsNullOrEmpty(x))
                        .ToList()
                    : null
            };
        }
        catch (Exception ex)
        {
            return new AnalysisResult
            {
                Intent = "분석 실패",
                Entities = new List<string>(),
                Confidence = 0.0,
                ErrorMessage = $"분석 응답 파싱 실패: {ex.Message}\n원본 응답: {response}"
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
