using System.Text.Json;
using AIAgentFramework.Core.Abstractions;
using AIAgentFramework.LLM.Abstractions;
using AIAgentFramework.LLM.Extensions;
using AIAgentFramework.LLM.Models;

namespace AIAgentFramework.LLM.Services.Validation;

public class ValidatorFunction : LLMFunctionBase<ValidationInput, ValidationResult>
{
    public ValidatorFunction(IPromptRegistry promptRegistry, ILLMProvider llmProvider, LLMFunctionOptions? options = null, ILogger? logger = null)
        : base(promptRegistry, llmProvider, options, logger) { }

    public override LLMRole Role => LLMRole.Validator;
    public override string Description => "데이터 검증 및 규칙 확인";
    protected override string GetPromptName() => "validation";

    protected override ValidationInput ExtractInput(ILLMContext context)
    {
        return new ValidationInput
        {
            Content = context.Get<string>("CONTENT") ?? throw new InvalidOperationException("CONTENT is required"),
            Schema = context.Get<string>("SCHEMA") ?? throw new InvalidOperationException("SCHEMA is required"),
            Rules = context.Get<string>("RULES")
        };
    }

    protected override IReadOnlyDictionary<string, object> PrepareVariables(ValidationInput input)
    {
        return new Dictionary<string, object>
        {
            ["CURRENT_TIME"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            ["CONTENT"] = input.Content,
            ["SCHEMA"] = input.Schema,
            ["RULES"] = input.Rules ?? "Standard validation rules"
        };
    }

    protected override ILLMResult CreateResult(string rawResponse, ValidationResult output)
    {
        return new LLMResult { Role = Role, RawResponse = rawResponse, ParsedData = output, IsSuccess = string.IsNullOrEmpty(output.ErrorMessage), ErrorMessage = output.ErrorMessage };
    }

    protected override ValidationResult ParseResponse(string response)
    {
        var cleanedResponse = CleanJsonResponse(response);
        try
        {
            var jsonDoc = JsonSerializer.Deserialize<JsonDocument>(cleanedResponse);
            var root = jsonDoc?.RootElement ?? throw new InvalidOperationException("검증 응답 파싱 실패");
            return new ValidationResult
            {
                IsValid = root.GetProperty("isValid").GetBoolean(),
                Errors = root.TryGetProperty("errors", out var errProp) ? errProp.EnumerateArray().Select(x => new ValidationError
                {
                    Field = x.GetProperty("field").GetString() ?? string.Empty,
                    Message = x.GetProperty("message").GetString() ?? string.Empty,
                    Severity = x.TryGetProperty("severity", out var sevProp) ? sevProp.GetString() : null
                }).ToList() : null,
                Warnings = root.TryGetProperty("warnings", out var warnProp) ? warnProp.EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToList() : null
            };
        }
        catch (Exception ex)
        {
            return new ValidationResult { IsValid = false, ErrorMessage = $"검증 응답 파싱 실패: {ex.Message}\n원본 응답: {response}" };
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
