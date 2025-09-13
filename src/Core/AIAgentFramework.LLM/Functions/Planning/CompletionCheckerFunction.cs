using AIAgentFramework.Core.Infrastructure;
using AIAgentFramework.Core.LLM.Abstractions;
using AIAgentFramework.Core.LLM.Attributes;
using AIAgentFramework.Core.LLM.Models;
using AIAgentFramework.Registry;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AIAgentFramework.LLM.Functions.Planning;

/// <summary>
/// 완료도 검사 LLM 기능
/// </summary>
[LLMFunction(
    name: "CompletionChecker",
    role: "completion_checker",
    description: "작업이나 프로젝트의 완료도를 평가하고 다음 단계를 제안하는 LLM 기능",
    SupportedModels = "gpt-4,gpt-4-turbo,claude-3-opus,claude-3-sonnet",
    RequiredParameters = "target_goal,current_status",
    OptionalParameters = "completion_criteria,quality_standards,context",
    Tags = "completion,evaluation,planning,quality",
    Priority = 85
)]
public class CompletionCheckerFunction : LLMFunctionBase
{
    /// <inheritdoc />
    public override string Name => "CompletionChecker";

    /// <inheritdoc />
    public override string Role => "completion_checker";

    /// <inheritdoc />
    public override string Description => "작업이나 프로젝트의 완료도를 평가하고 다음 단계를 제안하는 LLM 기능";

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="llmProvider">LLM Provider</param>
    /// <param name="promptManager">프롬프트 관리자</param>
    /// <param name="logger">로거</param>
    /// <param name="registry">Registry</param>
    public CompletionCheckerFunction(ILLMProvider llmProvider, IPromptManager promptManager, ILogger<CompletionCheckerFunction> logger, IAdvancedRegistry registry)
        : base(llmProvider, promptManager, logger, registry)
    {
    }

    /// <inheritdoc />
    protected override List<string> GetRequiredParameters()
    {
        return new List<string> { "target_goal", "current_status" };
    }

    /// <inheritdoc />
    protected override async Task<string> PreparePromptAsync(ILLMContext context, CancellationToken cancellationToken = default)
    {
        var parameters = PreparePromptParameters(context);
        
        // 기본값 설정
        if (!parameters.ContainsKey("completion_criteria"))
        {
            parameters["completion_criteria"] = "기능 완성도, 품질 기준 달성, 테스트 통과, 문서화 완료";
        }
        
        if (!parameters.ContainsKey("quality_standards"))
        {
            parameters["quality_standards"] = "오류 없음, 성능 기준 충족, 사용자 요구사항 만족, 유지보수 가능성";
        }
        
        if (!parameters.ContainsKey("context"))
        {
            parameters["context"] = "일반적인 완료도 평가 - 특별한 컨텍스트 없음";
        }

        return await _promptManager.LoadPromptAsync("completion_checker", parameters, cancellationToken);
    }

    /// <inheritdoc />
    protected override async Task<LLMResult> PostProcessAsync(string response, ILLMContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var model = context.Model ?? _llmProvider.DefaultModel;
            var tokensUsed = await CountTokensAsync(response, model);

            // JSON 파싱 시도
            CompletionCheckResponse? completionResponse = null;
            try
            {
                completionResponse = JsonSerializer.Deserialize<CompletionCheckResponse>(response, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true
                });
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse completion check response as JSON, treating as plain text");
            }

            var result = LLMResult.CreateSuccess(response, model, tokensUsed)
                .WithMetadata("function_type", "completion_checker")
                .WithMetadata("has_structured_data", completionResponse != null);

            if (completionResponse != null)
            {
                // 구조화된 데이터 메타데이터 추가
                result.WithMetadata("completion_percentage", completionResponse.CompletionPercentage)
                      .WithMetadata("is_fully_completed", completionResponse.IsFullyCompleted)
                      .WithMetadata("quality_score", completionResponse.QualityScore)
                      .WithMetadata("remaining_tasks_count", completionResponse.RemainingTasks.Count)
                      .WithMetadata("next_steps_count", completionResponse.NextSteps.Count);

                // 완료도 결과를 공유 데이터에 저장
                context.SharedData["completion_result"] = completionResponse;
                context.SharedData["completion_percentage"] = completionResponse.CompletionPercentage;
                context.SharedData["remaining_tasks"] = completionResponse.RemainingTasks;
                context.SharedData["next_steps"] = completionResponse.NextSteps;
                context.SharedData["is_project_completed"] = completionResponse.IsFullyCompleted;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to post-process completion check response");
            return LLMResult.CreateFailure($"Post-processing failed: {ex.Message}", context.Model ?? _llmProvider.DefaultModel);
        }
    }

    /// <inheritdoc />
    public override async Task<bool> ValidateAsync(ILLMContext context, CancellationToken cancellationToken = default)
    {
        // 기본 검증
        if (!await base.ValidateAsync(context, cancellationToken))
            return false;

        // 목표 검증
        var targetGoal = context.Parameters["target_goal"]?.ToString();
        if (string.IsNullOrWhiteSpace(targetGoal))
        {
            _logger.LogWarning("Target goal is required for completion checking");
            return false;
        }

        // 현재 상태 검증
        var currentStatus = context.Parameters["current_status"]?.ToString();
        if (string.IsNullOrWhiteSpace(currentStatus))
        {
            _logger.LogWarning("Current status is required for completion checking");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 완료도 퍼센테이지 계산
    /// </summary>
    /// <param name="completionResponse">완료도 응답</param>
    /// <returns>계산된 완료도 퍼센테이지</returns>
    public static int CalculateCompletionPercentage(CompletionCheckResponse completionResponse)
    {
        if (completionResponse == null)
            return 0;

        // 이미 완료도가 설정되어 있으면 반환
        if (completionResponse.CompletionPercentage > 0)
            return completionResponse.CompletionPercentage;

        // 완료된 작업과 전체 작업을 기반으로 계산
        var completedTasks = completionResponse.CompletedTasks.Count;
        var remainingTasks = completionResponse.RemainingTasks.Count;
        var totalTasks = completedTasks + remainingTasks;

        if (totalTasks == 0)
            return 100;

        return (int)Math.Round((double)completedTasks / totalTasks * 100);
    }

    /// <summary>
    /// 품질 점수 계산
    /// </summary>
    /// <param name="completionResponse">완료도 응답</param>
    /// <returns>계산된 품질 점수</returns>
    public static int CalculateQualityScore(CompletionCheckResponse completionResponse)
    {
        if (completionResponse == null)
            return 0;

        // 이미 품질 점수가 설정되어 있으면 반환
        if (completionResponse.QualityScore > 0)
            return completionResponse.QualityScore;

        // 품질 지표들을 기반으로 계산
        var qualityFactors = new[]
        {
            completionResponse.FunctionalityScore,
            completionResponse.ReliabilityScore,
            completionResponse.PerformanceScore,
            completionResponse.UsabilityScore,
            completionResponse.MaintainabilityScore
        };

        var validScores = qualityFactors.Where(score => score > 0).ToList();
        if (validScores.Count == 0)
            return 0;

        return (int)Math.Round(validScores.Average());
    }
}