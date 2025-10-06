using System.Text.Json;
using AIAgentFramework.Core.Abstractions;
using AIAgentFramework.LLM.Abstractions;
using AIAgentFramework.LLM.Extensions;
using AIAgentFramework.LLM.Models;

namespace AIAgentFramework.LLM.Services.Conversion;

/// <summary>
/// 변환 및 번역 LLM Function
/// </summary>
public class ConverterFunction : LLMFunctionBase<ConversionInput, ConversionResult>
{
    public ConverterFunction(
        IPromptRegistry promptRegistry,
        ILLMProvider llmProvider,
        LLMFunctionOptions? options = null,
        ILogger? logger = null)
        : base(promptRegistry, llmProvider, options, logger)
    {
    }

    public override LLMRole Role => LLMRole.Converter;

    public override string Description => "내용 변환 및 번역";

    protected override string GetPromptName() => "conversion";

    protected override ConversionInput ExtractInput(ILLMContext context)
    {
        return new ConversionInput
        {
            SourceContent = context.Get<string>("SOURCE_CONTENT")
                ?? throw new InvalidOperationException("SOURCE_CONTENT is required"),
            SourceFormat = context.Get<string>("SOURCE_FORMAT")
                ?? throw new InvalidOperationException("SOURCE_FORMAT is required"),
            TargetFormat = context.Get<string>("TARGET_FORMAT")
                ?? throw new InvalidOperationException("TARGET_FORMAT is required"),
            Options = context.Get<string>("OPTIONS")
        };
    }

    protected override IReadOnlyDictionary<string, object> PrepareVariables(ConversionInput input)
    {
        return new Dictionary<string, object>
        {
            ["CURRENT_TIME"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            ["SOURCE_CONTENT"] = input.SourceContent,
            ["SOURCE_FORMAT"] = input.SourceFormat,
            ["TARGET_FORMAT"] = input.TargetFormat,
            ["OPTIONS"] = input.Options ?? "Standard conversion"
        };
    }

    protected override ILLMResult CreateResult(string rawResponse, ConversionResult output)
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

    protected override ConversionResult ParseResponse(string response)
    {
        var cleanedResponse = CleanJsonResponse(response);

        try
        {
            var jsonDoc = JsonSerializer.Deserialize<JsonDocument>(cleanedResponse);
            var root = jsonDoc?.RootElement ?? throw new InvalidOperationException("변환 응답 파싱 실패");

            return new ConversionResult
            {
                ConvertedContent = root.GetProperty("convertedContent").GetString() ?? string.Empty,
                SourceFormat = root.GetProperty("sourceFormat").GetString() ?? string.Empty,
                TargetFormat = root.GetProperty("targetFormat").GetString() ?? string.Empty,
                QualityScore = root.GetProperty("qualityScore").GetDouble(),
                Warnings = root.TryGetProperty("warnings", out var warningsProp)
                    ? warningsProp.EnumerateArray()
                        .Select(x => x.GetString() ?? string.Empty)
                        .Where(x => !string.IsNullOrEmpty(x))
                        .ToList()
                    : null
            };
        }
        catch (Exception ex)
        {
            return new ConversionResult
            {
                ConvertedContent = "변환 실패",
                SourceFormat = "",
                TargetFormat = "",
                QualityScore = 0.0,
                ErrorMessage = $"변환 응답 파싱 실패: {ex.Message}\n원본 응답: {response}"
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
