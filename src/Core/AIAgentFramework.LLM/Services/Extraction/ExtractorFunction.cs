using System.Text.Json;
using AIAgentFramework.Core.Abstractions;
using AIAgentFramework.LLM.Abstractions;
using AIAgentFramework.LLM.Extensions;
using AIAgentFramework.LLM.Models;

namespace AIAgentFramework.LLM.Services.Extraction;

/// <summary>
/// 정보 추출 LLM Function
/// </summary>
public class ExtractorFunction : LLMFunctionBase<ExtractionInput, ExtractionResult>
{
    public ExtractorFunction(
        IPromptRegistry promptRegistry,
        ILLMProvider llmProvider,
        LLMFunctionOptions? options = null,
        ILogger? logger = null)
        : base(promptRegistry, llmProvider, options, logger)
    {
    }

    public override LLMRole Role => LLMRole.Extractor;

    public override string Description => "텍스트에서 특정 정보 추출";

    protected override string GetPromptName() => "extraction";

    protected override ExtractionInput ExtractInput(ILLMContext context)
    {
        return new ExtractionInput
        {
            SourceText = context.Get<string>("SOURCE_TEXT")
                ?? throw new InvalidOperationException("SOURCE_TEXT is required"),
            ExtractionType = context.Get<string>("EXTRACTION_TYPE")
                ?? throw new InvalidOperationException("EXTRACTION_TYPE is required"),
            Criteria = context.Get<string>("CRITERIA")
        };
    }

    protected override IReadOnlyDictionary<string, object> PrepareVariables(ExtractionInput input)
    {
        return new Dictionary<string, object>
        {
            ["CURRENT_TIME"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            ["SOURCE_TEXT"] = input.SourceText,
            ["EXTRACTION_TYPE"] = input.ExtractionType,
            ["CRITERIA"] = input.Criteria ?? "Standard extraction"
        };
    }

    protected override ILLMResult CreateResult(string rawResponse, ExtractionResult output)
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

    protected override ExtractionResult ParseResponse(string response)
    {
        var cleanedResponse = CleanJsonResponse(response);

        try
        {
            var jsonDoc = JsonSerializer.Deserialize<JsonDocument>(cleanedResponse);
            var root = jsonDoc?.RootElement ?? throw new InvalidOperationException("추출 응답 파싱 실패");

            var items = root.GetProperty("extractedItems").EnumerateArray()
                .Select(item => new ExtractedItem
                {
                    Value = item.GetProperty("value").GetString() ?? string.Empty,
                    Type = item.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : null,
                    Context = item.TryGetProperty("context", out var contextProp) ? contextProp.GetString() : null
                })
                .ToList();

            return new ExtractionResult
            {
                ExtractedItems = items,
                ExtractionType = root.GetProperty("extractionType").GetString() ?? string.Empty,
                TotalCount = root.GetProperty("totalCount").GetInt32(),
                Confidence = root.GetProperty("confidence").GetDouble()
            };
        }
        catch (Exception ex)
        {
            return new ExtractionResult
            {
                ExtractedItems = new List<ExtractedItem>(),
                ExtractionType = "",
                TotalCount = 0,
                Confidence = 0.0,
                ErrorMessage = $"추출 응답 파싱 실패: {ex.Message}\n원본 응답: {response}"
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
