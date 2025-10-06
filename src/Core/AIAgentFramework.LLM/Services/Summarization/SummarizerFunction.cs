using System.Text.Json;
using AIAgentFramework.Core.Abstractions;
using AIAgentFramework.LLM.Abstractions;
using AIAgentFramework.LLM.Extensions;
using AIAgentFramework.LLM.Models;

namespace AIAgentFramework.LLM.Services.Summarization;

/// <summary>
/// 텍스트 요약 LLM Function
/// </summary>
public class SummarizerFunction : LLMFunctionBase<SummarizationInput, SummarizationResult>
{
    public SummarizerFunction(
        IPromptRegistry promptRegistry,
        ILLMProvider llmProvider,
        LLMFunctionOptions? options = null,
        ILogger? logger = null)
        : base(promptRegistry, llmProvider, options, logger)
    {
    }

    public override LLMRole Role => LLMRole.Summarizer;

    public override string Description => "텍스트 내용 요약";

    protected override string GetPromptName() => "summarization";

    protected override SummarizationInput ExtractInput(ILLMContext context)
    {
        return new SummarizationInput
        {
            Content = context.Get<string>("CONTENT")
                ?? throw new InvalidOperationException("CONTENT is required"),
            SummaryStyle = context.Get<string>("SUMMARY_STYLE") ?? SummaryStyle.Standard,
            Requirements = context.Get<string>("REQUIREMENTS")
        };
    }

    protected override IReadOnlyDictionary<string, object> PrepareVariables(SummarizationInput input)
    {
        return new Dictionary<string, object>
        {
            ["CURRENT_TIME"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            ["CONTENT"] = input.Content,
            ["SUMMARY_STYLE"] = input.SummaryStyle,
            ["REQUIREMENTS"] = input.Requirements ?? "No additional requirements"
        };
    }

    protected override ILLMResult CreateResult(string rawResponse, SummarizationResult output)
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

    protected override SummarizationResult ParseResponse(string response)
    {
        var cleanedResponse = CleanJsonResponse(response);

        try
        {
            var jsonDoc = JsonSerializer.Deserialize<JsonDocument>(cleanedResponse);
            var root = jsonDoc?.RootElement ?? throw new InvalidOperationException("요약 응답 파싱 실패");

            return new SummarizationResult
            {
                Summary = root.GetProperty("summary").GetString() ?? string.Empty,
                Style = root.GetProperty("style").GetString() ?? SummaryStyle.Standard,
                KeyPoints = root.GetProperty("keyPoints").EnumerateArray()
                    .Select(x => x.GetString() ?? string.Empty)
                    .Where(x => !string.IsNullOrEmpty(x))
                    .ToList(),
                WordCount = root.GetProperty("wordCount").GetInt32(),
                OriginalLength = root.GetProperty("originalLength").GetInt32()
            };
        }
        catch (Exception ex)
        {
            return new SummarizationResult
            {
                Summary = "요약 생성 실패",
                Style = SummaryStyle.Standard,
                KeyPoints = new List<string>(),
                WordCount = 0,
                OriginalLength = 0,
                ErrorMessage = $"요약 응답 파싱 실패: {ex.Message}\n원본 응답: {response}"
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
