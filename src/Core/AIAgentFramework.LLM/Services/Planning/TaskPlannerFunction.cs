using System.Text.Json;
using AIAgentFramework.Core.Abstractions;
using AIAgentFramework.LLM.Abstractions;
using AIAgentFramework.LLM.Extensions;
using AIAgentFramework.LLM.Models;
using AIAgentFramework.LLM.Services.Universal;
using AIAgentFramework.Tools.Abstractions;

namespace AIAgentFramework.LLM.Services.Planning;

/// <summary>
/// 사용자 요청을 분석하여 실행 가능한 Task 계획을 생성하는 LLM 기능
/// </summary>
public class TaskPlannerFunction : LLMFunctionBase<PlanningInput, PlanningResult>
{
    private readonly IToolRegistry _toolRegistry;
    private readonly ILLMRegistry _llmRegistry;

    public TaskPlannerFunction(
        IPromptRegistry promptRegistry,
        ILLMProvider llmProvider,
        IToolRegistry toolRegistry,
        ILLMRegistry llmRegistry,
        LLMFunctionOptions? options = null,
        ILogger? logger = null)
        : base(promptRegistry, llmProvider, options, logger)
    {
        _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
        _llmRegistry = llmRegistry ?? throw new ArgumentNullException(nameof(llmRegistry));
    }

    public override LLMRole Role => LLMRole.Planner;

    public override string Description => "사용자 요청을 분석하여 실행 가능한 Task 계획 생성";

    protected override string GetPromptName() => "task-planning";

    protected override PlanningInput ExtractInput(ILLMContext context)
    {
        return new PlanningInput
        {
            UserRequest = context.UserInput,
            Context = context.Get<string>("CONTEXT"),
            ConversationHistory = context.Get<string>("HISTORY"),
            PreviousResults = context.Get<string>("PREVIOUS_RESULTS")
        };
    }

    protected override IReadOnlyDictionary<string, object> PrepareVariables(PlanningInput input)
    {
        var variables = new Dictionary<string, object>
        {
            ["TOOLS"] = _toolRegistry.GetToolDescriptionsForLLM(),
            ["LLM_FUNCTIONS"] = _llmRegistry.GetFunctionDescriptionsForLLM(),
            ["USER_REQUEST"] = input.UserRequest
        };

        if (!string.IsNullOrWhiteSpace(input.Context))
        {
            variables["CONTEXT"] = input.Context;
        }

        if (!string.IsNullOrWhiteSpace(input.ConversationHistory))
        {
            variables["HISTORY"] = input.ConversationHistory;
        }

        if (!string.IsNullOrWhiteSpace(input.PreviousResults))
        {
            variables["PREVIOUS_RESULTS"] = input.PreviousResults;
        }

        return variables;
    }

    protected override PlanningResult ParseResponse(string response)
    {
        var cleanedResponse = CleanJsonResponse(response);

        var jsonDoc = JsonSerializer.Deserialize<JsonDocument>(cleanedResponse);
        var root = jsonDoc?.RootElement ?? throw new InvalidOperationException("응답 파싱 실패");

        // Steps 파싱
        var steps = new List<TaskStep>();
        if (root.TryGetProperty("steps", out var stepsElement))
        {
            foreach (var stepElement in stepsElement.EnumerateArray())
            {
                // Parameters 파싱: 문자열 또는 객체 모두 허용
                string parametersJson;
                if (stepElement.TryGetProperty("parameters", out var paramsElement))
                {
                    parametersJson = paramsElement.ValueKind == JsonValueKind.String
                        ? paramsElement.GetString() ?? "{}"
                        : paramsElement.GetRawText(); // 객체면 그대로 JSON 문자열로
                }
                else
                {
                    parametersJson = "{}";
                }

                // ResponseGuide 파싱 (UniversalLLM 사용 시)
                ResponseGuide? responseGuide = null;
                if (stepElement.TryGetProperty("responseGuide", out var rgElement))
                {
                    responseGuide = new ResponseGuide
                    {
                        Instruction = rgElement.TryGetProperty("instruction", out var inst) ? inst.GetString() ?? "" : "",
                        Format = rgElement.TryGetProperty("format", out var fmt) ? fmt.GetString() ?? "JSON" : "JSON",
                        Style = rgElement.TryGetProperty("style", out var sty) ? sty.GetString() ?? "standard" : "standard",
                        Constraints = rgElement.TryGetProperty("constraints", out var cons)
                            ? cons.EnumerateArray().Select(c => c.GetString() ?? "").Where(s => !string.IsNullOrWhiteSpace(s)).ToList()
                            : new List<string>(),
                        OutputSchema = rgElement.TryGetProperty("outputSchema", out var schema) ? schema.GetString() : null,
                        Examples = rgElement.TryGetProperty("examples", out var ex)
                            ? ex.EnumerateArray().Select(e => e.GetString() ?? "").Where(s => !string.IsNullOrWhiteSpace(s)).ToList()
                            : null
                    };
                }

                var step = new TaskStep
                {
                    StepNumber = stepElement.GetProperty("stepNumber").GetInt32(),
                    Description = stepElement.GetProperty("description").GetString() ?? string.Empty,
                    ToolName = stepElement.GetProperty("toolName").GetString() ?? string.Empty,
                    Parameters = parametersJson,
                    OutputVariable = stepElement.TryGetProperty("outputVariable", out var outVar)
                        ? outVar.GetString()
                        : null,
                    EstimatedSeconds = stepElement.TryGetProperty("estimatedSeconds", out var estSec)
                        ? estSec.GetInt32()
                        : null,
                    ResponseGuide = responseGuide
                };

                // DependsOn 파싱 (숫자 또는 문자열 모두 허용)
                if (stepElement.TryGetProperty("dependsOn", out var depsElement))
                {
                    foreach (var dep in depsElement.EnumerateArray())
                    {
                        if (dep.ValueKind == JsonValueKind.Number)
                        {
                            step.DependsOn.Add(dep.GetInt32());
                        }
                        else if (dep.ValueKind == JsonValueKind.String)
                        {
                            // 문자열이면 무시 (변수명이 들어온 경우)
                            // dependsOn은 stepNumber만 유효하므로 숫자만 처리
                        }
                    }
                }

                steps.Add(step);
            }
        }

        // Constraints 파싱
        var constraints = new List<string>();
        if (root.TryGetProperty("constraints", out var constraintsElement))
        {
            foreach (var constraint in constraintsElement.EnumerateArray())
            {
                var constraintStr = constraint.GetString();
                if (!string.IsNullOrWhiteSpace(constraintStr))
                {
                    constraints.Add(constraintStr);
                }
            }
        }

        // Type 파싱
        var planType = PlanType.ToolExecution; // 기본값
        if (root.TryGetProperty("type", out var typeElement))
        {
            var typeStr = typeElement.GetString();
            if (Enum.TryParse<PlanType>(typeStr, ignoreCase: true, out var parsedType))
            {
                planType = parsedType;
            }
        }

        return new PlanningResult
        {
            Type = planType,
            Summary = root.GetProperty("summary").GetString() ?? string.Empty,
            DirectResponse = root.TryGetProperty("directResponse", out var directResp)
                ? directResp.GetString()
                : null,
            Steps = steps,
            TotalEstimatedSeconds = root.TryGetProperty("totalEstimatedSeconds", out var totalSec)
                ? totalSec.GetInt32()
                : 0,
            IsExecutable = root.TryGetProperty("isExecutable", out var isExec)
                ? isExec.GetBoolean()
                : true,
            ExecutionBlocker = root.TryGetProperty("executionBlocker", out var blocker)
                ? blocker.GetString()
                : null,
            Constraints = constraints
        };
    }

    protected override ILLMResult CreateResult(string rawResponse, PlanningResult output)
    {
        return new LLMResult
        {
            Role = Role,
            RawResponse = rawResponse,
            ParsedData = output,
            IsSuccess = output.IsExecutable,
            ErrorMessage = output.ExecutionBlocker
        };
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
