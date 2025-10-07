using System.Text.Json;
using AIAgentFramework.Core.Abstractions;
using AIAgentFramework.LLM.Abstractions;
using AIAgentFramework.LLM.Extensions;
using AIAgentFramework.LLM.Models;

namespace AIAgentFramework.LLM.Services.Universal;

/// <summary>
/// 범용 LLM Function
/// Persona와 ResponseGuide를 받아 동적으로 프롬프트를 구성하고 모든 LLM 작업 수행
/// </summary>
public class UniversalLLMFunction : LLMFunctionBase<UniversalLLMInput, UniversalLLMResult>
{
    public UniversalLLMFunction(
        IPromptRegistry promptRegistry,
        ILLMProvider llmProvider,
        LLMFunctionOptions? options = null,
        ILogger? logger = null)
        : base(promptRegistry, llmProvider, options, logger)
    {
    }

    public override LLMRole Role => LLMRole.Universal;

    public override string Description => "범용 LLM - 동적 프롬프트로 모든 작업 수행";

    protected override string GetPromptName() => "universal-llm";

    protected override UniversalLLMInput ExtractInput(ILLMContext context)
    {
        // TaskType은 필수
        var taskType = context.Get<string>("TASK_TYPE");
        if (string.IsNullOrWhiteSpace(taskType))
        {
            throw new InvalidOperationException("TASK_TYPE is required for UniversalLLM");
        }

        // Content 추출 (다양한 이름 시도)
        var content = TryGetParameter(context, "CONTENT", "TEXT", "DATA", "INPUT", "SOURCE");
        if (string.IsNullOrWhiteSpace(content))
        {
            // 파라미터에 없으면 첫 번째 값 시도
            content = context.Parameters
                .Where(p => !IsReservedParameter(p.Key))
                .Select(p => p.Value?.ToString())
                .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("Content is required for UniversalLLM");
        }

        // ResponseGuide 추출
        var responseGuideJson = context.Get<string>("RESPONSE_GUIDE");
        ResponseGuide? responseGuide = null;

        if (!string.IsNullOrWhiteSpace(responseGuideJson))
        {
            try
            {
                responseGuide = JsonSerializer.Deserialize<ResponseGuide>(responseGuideJson);
            }
            catch
            {
                // 파싱 실패 시 기본 가이드 사용
            }
        }

        return new UniversalLLMInput
        {
            TaskType = taskType,
            Content = content,
            Persona = context.Get<string>("PERSONA"),
            ResponseGuide = responseGuide ?? new ResponseGuide
            {
                Instruction = $"Perform {taskType} task",
                Format = "JSON",
                Style = "standard"
            },
            AdditionalContext = context.Get<string>("ADDITIONAL_CONTEXT")
        };
    }

    protected override IReadOnlyDictionary<string, object> PrepareVariables(UniversalLLMInput input)
    {
        var variables = new Dictionary<string, object>
        {
            ["TASK_TYPE"] = input.TaskType,
            ["CONTENT"] = input.Content,
            ["PERSONA"] = input.Persona ?? "Professional AI Assistant",
            ["INSTRUCTION"] = input.ResponseGuide.Instruction,
            ["FORMAT"] = input.ResponseGuide.Format,
            ["OUTPUT_SCHEMA"] = input.ResponseGuide.OutputSchema ?? string.Empty,
            ["STYLE"] = input.ResponseGuide.Style,
            ["CONSTRAINTS"] = input.ResponseGuide.Constraints,
            ["EXAMPLES"] = input.ResponseGuide.Examples ?? new List<string>(),
            ["ADDITIONAL_CONTEXT"] = input.AdditionalContext ?? string.Empty
        };

        return variables;
    }

    protected override ILLMResult CreateResult(string rawResponse, UniversalLLMResult output)
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

    protected override UniversalLLMResult ParseResponse(string response)
    {
        var cleanedResponse = CleanJsonResponse(response);

        try
        {
            // JSON 형식인 경우 파싱 시도
            var jsonDoc = JsonSerializer.Deserialize<JsonDocument>(cleanedResponse);
            var root = jsonDoc?.RootElement;

            if (root.HasValue && root.Value.TryGetProperty("response", out var responseElement))
            {
                return new UniversalLLMResult
                {
                    Response = responseElement.GetString() ?? cleanedResponse,
                    TaskType = root.Value.TryGetProperty("taskType", out var taskTypeElem)
                        ? taskTypeElem.GetString() ?? "Unknown"
                        : "Unknown",
                    Persona = root.Value.TryGetProperty("persona", out var personaElem)
                        ? personaElem.GetString()
                        : null,
                    QualityScore = root.Value.TryGetProperty("qualityScore", out var qualityElem)
                        ? qualityElem.GetDouble()
                        : 1.0
                };
            }
        }
        catch
        {
            // JSON 파싱 실패 시 전체를 응답으로 사용
        }

        // JSON이 아니거나 파싱 실패 시 원본을 그대로 응답으로 사용
        return new UniversalLLMResult
        {
            Response = cleanedResponse,
            TaskType = "Unknown",
            QualityScore = 1.0
        };
    }

    private string? TryGetParameter(ILLMContext context, params string[] parameterNames)
    {
        foreach (var paramName in parameterNames)
        {
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

    private bool IsReservedParameter(string key)
    {
        var reserved = new[]
        {
            "TASK_TYPE", "PERSONA", "RESPONSE_GUIDE", "ADDITIONAL_CONTEXT",
            "HISTORY", "CONTEXT", "USER_", "SESSION_", "EXECUTION_"
        };

        return reserved.Any(r => key.Equals(r, StringComparison.OrdinalIgnoreCase) ||
                                 key.StartsWith(r, StringComparison.OrdinalIgnoreCase));
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
