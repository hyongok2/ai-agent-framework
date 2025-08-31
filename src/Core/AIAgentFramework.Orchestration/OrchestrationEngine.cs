using AIAgentFramework.Core.Actions.Abstractions;
using AIAgentFramework.Core.Actions.Factories;
using AIAgentFramework.Core.Infrastructure;
using AIAgentFramework.Core.LLM.Abstractions;
using AIAgentFramework.Core.LLM.Models;
using AIAgentFramework.Core.Orchestration.Abstractions;
using AIAgentFramework.Core.User;
using Microsoft.Extensions.Logging;

namespace AIAgentFramework.Orchestration;

/// <summary>
/// 오케스트레이션 엔진 구현
/// </summary>
public class OrchestrationEngine : IOrchestrationEngine
{
    private readonly IRegistry _registry;
    private readonly IActionFactory _actionFactory;
    private readonly ILogger<OrchestrationEngine> _logger;

    public OrchestrationEngine(IRegistry registry, IActionFactory actionFactory, ILogger<OrchestrationEngine> logger)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _actionFactory = actionFactory ?? throw new ArgumentNullException(nameof(actionFactory));
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
        var context = new OrchestrationContext(userRequest, _registry);
        
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

        var actionsData = context.SharedData["plan_actions"];
        
        // JSON 배열에서 타입 안전한 액션으로 변환
        List<IOrchestrationAction> actions;
        try
        {
            if (actionsData is List<Dictionary<string, object>> actionDictList)
            {
                actions = _actionFactory.CreateActionsFromJsonArray(actionDictList);
            }
            else
            {
                _logger.LogWarning("Invalid action data format in plan_actions");
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse actions from plan_actions");
            return;
        }

        foreach (var action in actions)
        {
            if (cancellationToken.IsCancellationRequested) break;
            
            await ExecuteActionAsync(context, action, cancellationToken);
        }
    }

    private async Task ExecuteActionAsync(OrchestrationContext context, IOrchestrationAction action, CancellationToken cancellationToken)
    {
        var step = new ExecutionStep
        {
            StepType = action.Type.ToString(),
            Description = $"액션 실행: {action.Name}",
            Input = action.Name,
            StartTime = DateTime.UtcNow
        };

        try
        {
            _logger.LogDebug("액션 실행 시작: {ActionName} (타입: {ActionType})", action.Name, action.Type);
            
            var result = await action.ExecuteAsync(context, cancellationToken);
            
            step.Success = result.IsSuccess;
            step.Output = result.Data?.ToString() ?? "";
            step.ExecutionTime = result.ExecutionTime;
            step.EndTime = DateTime.UtcNow;
            
            if (!result.IsSuccess)
            {
                step.ErrorMessage = result.ErrorMessage;
                _logger.LogWarning("액션 실행 실패: {ActionName} - {ErrorMessage}", action.Name, result.ErrorMessage);
            }
            else
            {
                _logger.LogDebug("액션 실행 성공: {ActionName}", action.Name);
            }
        }
        catch (Exception ex)
        {
            step.Success = false;
            step.ErrorMessage = ex.Message;
            step.EndTime = DateTime.UtcNow;
            _logger.LogError(ex, "액션 실행 중 예외 발생: {ActionName}", action.Name);
        }
        
        context.AddExecutionStep(step);
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

}