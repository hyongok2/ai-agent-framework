using AIAgentFramework.Core.Interfaces;
using AIAgentFramework.Core.Models;
using Microsoft.Extensions.Logging;

namespace AIAgentFramework.Orchestration;

/// <summary>
/// 오케스트레이션 엔진 구현
/// </summary>
public class OrchestrationEngine : IOrchestrationEngine
{
    private readonly IRegistry _registry;
    private readonly ILogger<OrchestrationEngine> _logger;

    public OrchestrationEngine(IRegistry registry, ILogger<OrchestrationEngine> logger)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IOrchestrationResult> ExecuteAsync(IUserRequest userRequest)
    {
        var context = await ExecuteAsync(userRequest.Content);
        return new OrchestrationResult(context);
    }
    
    /// <summary>
    /// 오케스트레이션 실행 (문자열 요청)
    /// </summary>
    public async Task<IOrchestrationContext> ExecuteAsync(string userRequest, CancellationToken cancellationToken = default)
    {
        var context = new OrchestrationContext(userRequest);
        
        try
        {
            _logger.LogInformation("오케스트레이션 시작: {SessionId}", context.SessionId);
            
            while (!context.IsCompleted && !cancellationToken.IsCancellationRequested)
            {
                // 1. 계획 수립
                var planResult = await ExecutePlanningAsync(context, cancellationToken);
                if (!planResult.Success)
                {
                    context.SetError($"계획 수립 실패: {planResult.ErrorMessage}");
                    break;
                }

                // 2. 계획된 액션 실행
                await ExecutePlannedActionsAsync(context, cancellationToken);
                
                // 3. 완료 조건 확인
                await CheckCompletionAsync(context, cancellationToken);
            }

            _logger.LogInformation("오케스트레이션 완료: {SessionId}, 단계: {StepCount}", 
                context.SessionId, context.ExecutionHistory.Count);
            
            return context;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "오케스트레이션 실행 중 오류 발생: {SessionId}", context.SessionId);
            context.SetError($"실행 오류: {ex.Message}");
            return context;
        }
    }

    /// <inheritdoc />
    public async Task<IOrchestrationResult> ContinueAsync(IOrchestrationContext context)
    {
        if (context.IsCompleted)
        {
            _logger.LogWarning("이미 완료된 컨텍스트입니다: {SessionId}", context.SessionId);
            return new OrchestrationResult(context);
        }

        var updatedContext = await ExecuteAsync((context as OrchestrationContext)?.UserRequest ?? "");
        return new OrchestrationResult(updatedContext);
    }

    private async Task<ILLMResult> ExecutePlanningAsync(OrchestrationContext context, CancellationToken cancellationToken)
    {
        var planner = _registry.GetLLMFunction("planner");
        if (planner == null)
        {
            throw new InvalidOperationException("Planner 기능을 찾을 수 없습니다.");
        }

        var llmContext = new LLMContext
        {
            UserRequest = context.UserRequest,
            ExecutionHistory = context.ExecutionHistory,
            SharedData = context.SharedData
        };
        llmContext.Parameters["user_request"] = context.UserRequest;

        var result = await planner.ExecuteAsync(llmContext, cancellationToken);
        
        var step = new ExecutionStep
        {
            StepType = "Planning",
            Description = "사용자 요청 분석 및 실행 계획 수립",
            Input = context.UserRequest,
            Output = result.Content,
            Success = result.Success,
            ExecutionTime = result.ExecutionTime
        };
        
        context.AddExecutionStep(step);
        return result;
    }

    private async Task ExecutePlannedActionsAsync(OrchestrationContext context, CancellationToken cancellationToken)
    {
        if (!context.SharedData.ContainsKey("plan_actions"))
            return;

        var actions = context.SharedData["plan_actions"] as List<object>;
        if (actions == null) return;

        foreach (var action in actions)
        {
            if (cancellationToken.IsCancellationRequested) break;
            
            await ExecuteActionAsync(context, action, cancellationToken);
        }
    }

    private async Task ExecuteActionAsync(OrchestrationContext context, object action, CancellationToken cancellationToken)
    {
        // 액션 타입에 따른 실행 분기
        var actionType = GetActionType(action);
        
        var step = new ExecutionStep
        {
            StepType = actionType,
            Description = $"액션 실행: {actionType}",
            Input = action.ToString() ?? "",
            StartTime = DateTime.UtcNow
        };

        try
        {
            if (actionType.StartsWith("llm_"))
            {
                await ExecuteLLMActionAsync(context, action, step, cancellationToken);
            }
            else if (actionType.StartsWith("tool_"))
            {
                await ExecuteToolActionAsync(context, action, step, cancellationToken);
            }
            
            step.Success = true;
            step.EndTime = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            step.Success = false;
            step.ErrorMessage = ex.Message;
            step.EndTime = DateTime.UtcNow;
            _logger.LogError(ex, "액션 실행 실패: {ActionType}", actionType);
        }
        
        context.AddExecutionStep(step);
    }

    private async Task ExecuteLLMActionAsync(OrchestrationContext context, object action, ExecutionStep step, CancellationToken cancellationToken)
    {
        var functionName = ExtractFunctionName(action);
        var llmFunction = _registry.GetLLMFunction(functionName);
        
        if (llmFunction == null)
        {
            throw new InvalidOperationException($"LLM 기능을 찾을 수 없습니다: {functionName}");
        }

        var llmContext = new LLMContext
        {
            ExecutionHistory = context.ExecutionHistory,
            SharedData = context.SharedData
        };
        
        var result = await llmFunction.ExecuteAsync(llmContext, cancellationToken);
        step.Output = result.Content;
        step.ExecutionTime = result.ExecutionTime;
    }

    private async Task ExecuteToolActionAsync(OrchestrationContext context, object action, ExecutionStep step, CancellationToken cancellationToken)
    {
        var toolName = ExtractToolName(action);
        var tool = _registry.GetTool(toolName);
        
        if (tool == null)
        {
            throw new InvalidOperationException($"도구를 찾을 수 없습니다: {toolName}");
        }

        var toolInput = new ToolInput();
        var result = await tool.ExecuteAsync(toolInput, cancellationToken);
        step.Output = result.Data?.ToString() ?? "";
        step.ExecutionTime = result.ExecutionTime;
    }

    private async Task CheckCompletionAsync(OrchestrationContext context, CancellationToken cancellationToken)
    {
        var completionChecker = new Completion.CompletionChecker();
        context.IsCompleted = completionChecker.IsCompleted(context);
        
        if (context.IsCompleted)
        {
            context.CompletedAt = DateTime.UtcNow;
            _logger.LogInformation("오케스트레이션 완료 조건 충족: {SessionId}", context.SessionId);
        }
        
        await Task.CompletedTask;
    }

    private static string GetActionType(object action)
    {
        // 액션 객체에서 타입 추출
        return action.ToString()?.Split('_')[0] ?? "unknown";
    }

    private static string ExtractFunctionName(object action)
    {
        return action.ToString() ?? "";
    }

    private static string ExtractToolName(object action)
    {
        return action.ToString() ?? "";
    }
}