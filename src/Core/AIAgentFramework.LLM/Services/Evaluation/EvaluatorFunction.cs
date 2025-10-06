using System.Text.Json;
using AIAgentFramework.Core.Abstractions;
using AIAgentFramework.LLM.Abstractions;
using AIAgentFramework.LLM.Extensions;
using AIAgentFramework.LLM.Models;

namespace AIAgentFramework.LLM.Services.Evaluation;

/// <summary>
/// 실행 결과 평가 및 품질 검증 LLM Function
/// </summary>
public class EvaluatorFunction : LLMFunctionBase<EvaluationInput, EvaluationResult>
{
    public EvaluatorFunction(
        IPromptRegistry promptRegistry,
        ILLMProvider llmProvider,
        LLMFunctionOptions? options = null,
        ILogger? logger = null)
        : base(promptRegistry, llmProvider, options, logger)
    {
    }

    public override LLMRole Role => LLMRole.Evaluator;

    public override string Description => "실행 결과의 품질 평가 및 검증";

    protected override string GetPromptName() => "evaluation";

    protected override EvaluationInput ExtractInput(ILLMContext context)
    {
        return new EvaluationInput
        {
            TaskDescription = context.Get<string>("TASK_DESCRIPTION") ?? throw new InvalidOperationException("TASK_DESCRIPTION is required"),
            ExecutionResult = context.Get<string>("EXECUTION_RESULT") ?? throw new InvalidOperationException("EXECUTION_RESULT is required"),
            ExpectedOutcome = context.Get<string>("EXPECTED_OUTCOME"),
            EvaluationCriteria = context.Get<string>("EVALUATION_CRITERIA")
        };
    }

    protected override IReadOnlyDictionary<string, object> PrepareVariables(EvaluationInput input)
    {
        return new Dictionary<string, object>
        {
            ["CURRENT_TIME"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            ["TASK_DESCRIPTION"] = input.TaskDescription,
            ["EXECUTION_RESULT"] = input.ExecutionResult,
            ["EXPECTED_OUTCOME"] = input.ExpectedOutcome ?? "Not specified",
            ["EVALUATION_CRITERIA"] = input.EvaluationCriteria ?? "Standard quality criteria: correctness, completeness, accuracy, format compliance"
        };
    }

    protected override ILLMResult CreateResult(string rawResponse, EvaluationResult output)
    {
        return new LLMResult
        {
            Role = Role,
            RawResponse = rawResponse,
            ParsedData = output,
            IsSuccess = output.IsSuccess,
            ErrorMessage = output.ErrorMessage
        };
    }

    protected override EvaluationResult ParseResponse(string response)
    {
        var cleanedResponse = CleanJsonResponse(response);

        try
        {
            var jsonDoc = JsonSerializer.Deserialize<JsonDocument>(cleanedResponse);
            var root = jsonDoc?.RootElement ?? throw new InvalidOperationException("평가 응답 파싱 실패");

            return new EvaluationResult
            {
                IsSuccess = root.GetProperty("isSuccess").GetBoolean(),
                QualityScore = root.GetProperty("qualityScore").GetDouble(),
                Assessment = root.GetProperty("assessment").GetString() ?? string.Empty,
                Strengths = root.GetProperty("strengths").EnumerateArray()
                    .Select(x => x.GetString() ?? string.Empty)
                    .Where(x => !string.IsNullOrEmpty(x))
                    .ToList(),
                Weaknesses = root.GetProperty("weaknesses").EnumerateArray()
                    .Select(x => x.GetString() ?? string.Empty)
                    .Where(x => !string.IsNullOrEmpty(x))
                    .ToList(),
                Recommendations = root.GetProperty("recommendations").EnumerateArray()
                    .Select(x => x.GetString() ?? string.Empty)
                    .Where(x => !string.IsNullOrEmpty(x))
                    .ToList(),
                MeetsCriteria = root.GetProperty("meetsCriteria").GetBoolean()
            };
        }
        catch (Exception ex)
        {
            return new EvaluationResult
            {
                IsSuccess = false,
                QualityScore = 0.0,
                Assessment = "평가 실패",
                Strengths = new List<string>(),
                Weaknesses = new List<string> { "응답 파싱 실패" },
                Recommendations = new List<string>(),
                MeetsCriteria = false,
                ErrorMessage = $"평가 응답 파싱 실패: {ex.Message}\n원본 응답: {response}"
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
