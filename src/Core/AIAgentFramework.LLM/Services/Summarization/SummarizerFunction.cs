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
        string? content = null;

        // 1. 다양한 파라미터 이름 시도 (대소문자 무시)
        content = TryGetParameter(context, "CONTENT", "TEXT", "DATA", "INPUT", "SOURCE");

        // 2. 파라미터가 없으면 이전 Step 결과 시도
        if (string.IsNullOrWhiteSpace(content) && context.Parameters.Count > 0)
        {
            var firstValue = context.Parameters
                .Where(p => !p.Key.Equals("SUMMARY_STYLE", StringComparison.OrdinalIgnoreCase))
                .Where(p => !p.Key.Equals("REQUIREMENTS", StringComparison.OrdinalIgnoreCase))
                .Where(p => !p.Key.Equals("HISTORY", StringComparison.OrdinalIgnoreCase))
                .Where(p => !p.Key.StartsWith("USER_", StringComparison.OrdinalIgnoreCase))
                .Where(p => !p.Key.StartsWith("SESSION_", StringComparison.OrdinalIgnoreCase))
                .Where(p => !p.Key.StartsWith("EXECUTION_", StringComparison.OrdinalIgnoreCase))
                .Select(p => p.Value?.ToString())
                .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

            if (!string.IsNullOrWhiteSpace(firstValue))
            {
                content = firstValue;
            }
        }

        // 3. 그래도 없으면 사용자 입력(UserInput) 시도
        if (string.IsNullOrWhiteSpace(content))
        {
            content = context.UserInput;
        }

        // 4. 최종적으로 없으면 에러
        if (string.IsNullOrWhiteSpace(content))
        {
            var availableParams = string.Join(", ", context.Parameters.Keys);
            throw new InvalidOperationException(
                $"CONTENT is required. Tried: " +
                $"1) Parameter names: CONTENT, TEXT, DATA, INPUT, SOURCE. " +
                $"2) Previous step results. " +
                $"3) User input (context.UserInput). " +
                $"Available parameters: {availableParams}");
        }

        return new SummarizationInput
        {
            Content = content,
            SummaryStyle = context.Get<string>("SUMMARY_STYLE") ?? SummaryStyle.Standard,
            Requirements = context.Get<string>("REQUIREMENTS")
        };
    }

    /// <summary>
    /// 여러 가능한 파라미터 이름을 시도하여 값을 가져옴
    /// </summary>
    private string? TryGetParameter(ILLMContext context, params string[] parameterNames)
    {
        foreach (var paramName in parameterNames)
        {
            // 대소문자 무시하고 찾기
            var value = context.Parameters
                .Where(p => p.Key.Equals(paramName, StringComparison.OrdinalIgnoreCase))
                .Select(p => p.Value?.ToString())
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
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
