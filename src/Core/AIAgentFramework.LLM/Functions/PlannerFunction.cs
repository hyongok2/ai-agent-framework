using AIAgentFramework.Core.Infrastructure;
using AIAgentFramework.Core.LLM.Abstractions;
using AIAgentFramework.Core.LLM.Attributes;
using AIAgentFramework.Core.LLM.Models;
using AIAgentFramework.LLM.Parsing;
using AIAgentFramework.LLM.Parsing.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AIAgentFramework.LLM.Functions;

/// <summary>
/// 계획 수립 LLM 기능
/// </summary>
[LLMFunction(
    name: "Planner",
    role: "planner",
    description: "사용자 요구사항을 분석하고 실행 계획을 수립하는 LLM 기능",
    SupportedModels = "gpt-4,gpt-4-turbo,claude-3-opus,claude-3-sonnet",
    RequiredParameters = "user_request",
    OptionalParameters = "available_llm_functions,available_tools,context",
    Tags = "planning,analysis,orchestration",
    Priority = 100
)]
public class PlannerFunction : LLMFunctionBase
{
    private readonly ILLMResponseParser _responseParser;

    /// <inheritdoc />
    public override string Name => "Planner";

    /// <inheritdoc />
    public override string Role => "planner";

    /// <inheritdoc />
    public override string Description => "사용자 요구사항을 분석하고 실행 계획을 수립하는 LLM 기능";

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="llmProvider">LLM Provider</param>
    /// <param name="promptManager">프롬프트 관리자</param>
    /// <param name="responseParser">응답 파서</param>
    /// <param name="logger">로거</param>
    public PlannerFunction(ILLMProvider llmProvider, IPromptManager promptManager, ILLMResponseParser responseParser, ILogger<PlannerFunction> logger)
        : base(llmProvider, promptManager, logger)
    {
        _responseParser = responseParser ?? throw new ArgumentNullException(nameof(responseParser));
    }

    /// <inheritdoc />
    protected override List<string> GetRequiredParameters()
    {
        return new List<string> { "user_request" };
    }

    /// <inheritdoc />
    protected override async Task<string> PreparePromptAsync(ILLMContext context, CancellationToken cancellationToken = default)
    {
        var parameters = PreparePromptParameters(context);
        
        // 기본값 설정
        if (!parameters.ContainsKey("available_llm_functions"))
        {
            parameters["available_llm_functions"] = "interpreter, summarizer, generator, evaluator, rewriter, explainer, reasoner, converter, visualizer, tool_parameter_setter, dialogue_manager, knowledge_retriever, meta_manager";
        }
        
        if (!parameters.ContainsKey("available_tools"))
        {
            parameters["available_tools"] = "embedding_cache, vector_db, web_search, file_processor, database_query";
        }
        
        if (!parameters.ContainsKey("context"))
        {
            parameters["context"] = "새로운 요청 - 이전 컨텍스트 없음";
        }

        return await _promptManager.LoadPromptAsync("planner", parameters, cancellationToken);
    }

    /// <inheritdoc />
    protected override async Task<LLMResult> PostProcessAsync(string response, ILLMContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var model = context.Model ?? _llmProvider.DefaultModel;
            var tokensUsed = await CountTokensAsync(response, model);

            // 응답 파서를 사용하여 파싱
            var planResponse = _responseParser.ParseJson<PlannerResponse>(response);
            
            var result = LLMResult.CreateSuccess(response, model, tokensUsed)
                .WithMetadata("function_type", "planner")
                .WithMetadata("has_structured_data", planResponse != null);

            if (planResponse != null)
            {
                // 구조화된 데이터 메타데이터 추가
                result.WithMetadata("plan_analysis", planResponse.Analysis)
                      .WithMetadata("plan_goal", planResponse.Goal)
                      .WithMetadata("actions_count", planResponse.Actions.Count)
                      .WithMetadata("is_completed", planResponse.IsCompleted);

                // 다음 실행을 위해 공유 데이터에 계획 저장
                context.SharedData["current_plan"] = planResponse;
                context.SharedData["plan_actions"] = planResponse.Actions;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to post-process planner response");
            return LLMResult.CreateFailure($"Post-processing failed: {ex.Message}", context.Model ?? _llmProvider.DefaultModel);
        }
    }

    /// <inheritdoc />
    public override async Task<bool> ValidateAsync(ILLMContext context, CancellationToken cancellationToken = default)
    {
        // 기본 검증
        if (!await base.ValidateAsync(context, cancellationToken))
            return false;

        // 사용자 요청이 의미있는 내용인지 확인
        var userRequest = context.Parameters["user_request"]?.ToString();
        if (string.IsNullOrWhiteSpace(userRequest) || userRequest.Length < 3)
        {
            _logger.LogWarning("User request is too short or empty");
            return false;
        }

        return true;
    }


}