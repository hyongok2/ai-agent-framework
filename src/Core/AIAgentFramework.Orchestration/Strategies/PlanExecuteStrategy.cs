
using AIAgentFramework.Core.Common.Registry;
using AIAgentFramework.Core.LLM.Abstractions;
using AIAgentFramework.Core.LLM.Models;
using AIAgentFramework.Core.Orchestration.Abstractions;
using AIAgentFramework.Core.Tools.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AIAgentFramework.Orchestration.Strategies;

/// <summary>
/// 계획-실행 전략 구현
/// 1. 계획 수립 → 2. 실행 → 3. 검증 → 반복
/// </summary>
public class PlanExecuteStrategy : IOrchestrationStrategy
{
    private readonly IRegistry _registry;
    private readonly ILogger<PlanExecuteStrategy> _logger;
    private readonly int _maxIterations;

    public PlanExecuteStrategy(
        IRegistry registry,
        ILogger<PlanExecuteStrategy> logger,
        int maxIterations = 10)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _maxIterations = maxIterations;
    }

    public string Name => "PlanExecute";
    
    public string Description => "계획을 수립하고 단계별로 실행하는 기본 전략";
    
    public int Priority => 100;

    public bool CanHandle(IOrchestrationContext context)
    {
        // 기본 전략으로 모든 컨텍스트 처리 가능
        return true;
    }

    public async Task<IOrchestrationResult> ExecuteAsync(
        IOrchestrationContext context,
        CancellationToken cancellationToken = default)
    {
        var orchestrationContext = context as OrchestrationContext 
            ?? throw new InvalidCastException("Invalid context type");
        
        try
        {
            _logger.LogInformation("[PlanExecute] 전략 시작: {SessionId}", context.SessionId);
            
            int iteration = 0;
            while (!orchestrationContext.IsCompleted && iteration < _maxIterations)
            {
                cancellationToken.ThrowIfCancellationRequested();
                iteration++;
                
                _logger.LogDebug("[PlanExecute] Iteration {Iteration}/{Max}", iteration, _maxIterations);
                
                // 1단계: 계획 수립
                var plan = await CreatePlanAsync(orchestrationContext, cancellationToken);
                if (!plan.Success)
                {
                    _logger.LogError("[PlanExecute] 계획 수립 실패: {Error}", plan.ErrorMessage);
                    orchestrationContext.SetError($"계획 수립 실패: {plan.ErrorMessage}");
                    break;
                }
                
                // 2단계: 계획 실행
                var executionResult = await ExecutePlanAsync(orchestrationContext, plan, cancellationToken);
                if (!executionResult.Success)
                {
                    _logger.LogWarning("[PlanExecute] 계획 실행 부분 실패, 재계획 필요");
                    // 실패해도 계속 진행 (재계획 수립)
                }
                
                // 3단계: 완료 여부 확인
                var isComplete = await CheckCompletionAsync(orchestrationContext, cancellationToken);
                if (isComplete)
                {
                    orchestrationContext.IsCompleted = true;
                    orchestrationContext.CompletedAt = DateTime.UtcNow;
                    _logger.LogInformation("[PlanExecute] 목표 달성 완료: {SessionId}", context.SessionId);
                }
            }
            
            if (iteration >= _maxIterations)
            {
                _logger.LogWarning("[PlanExecute] 최대 반복 횟수 도달: {MaxIterations}", _maxIterations);
                orchestrationContext.SetError($"최대 반복 횟수({_maxIterations}) 초과");
            }
            
            return new OrchestrationResult(orchestrationContext);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("[PlanExecute] 작업 취소됨");
            orchestrationContext.SetError("작업이 취소되었습니다");
            return new OrchestrationResult(orchestrationContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PlanExecute] 전략 실행 중 오류");
            orchestrationContext.SetError($"전략 실행 오류: {ex.Message}");
            return new OrchestrationResult(orchestrationContext);
        }
    }

    private async Task<ILLMResult> CreatePlanAsync(
        OrchestrationContext context,
        CancellationToken cancellationToken)
    {
        var planner = _registry.GetLLMFunction("planner");
        if (planner == null)
        {
            throw new InvalidOperationException("Planner LLM 기능을 찾을 수 없습니다");
        }
        
        var llmContext = new LLMContext
        {
            UserRequest = context.UserRequest,
            ExecutionHistory = context.ExecutionHistory,
            SharedData = context.SharedData
        };
        
        // 이전 실행 결과를 컨텍스트에 포함
        if (context.ExecutionHistory.Any())
        {
            llmContext.Parameters["previous_steps"] = JsonSerializer.Serialize(
                context.ExecutionHistory.Select(h => new
                {
                    h.StepType,
                    h.Description,
                    Success = h.IsSuccess,
                    h.Output
                }));
        }
        
        llmContext.Parameters["user_request"] = context.UserRequest;
        llmContext.Parameters["max_steps"] = "5"; // 한 번에 최대 5단계 계획
        
        return await planner.ExecuteAsync(llmContext, cancellationToken);
    }

    private async Task<ExecutionResult> ExecutePlanAsync(
        OrchestrationContext context,
        ILLMResult plan,
        CancellationToken cancellationToken)
    {
        var result = new ExecutionResult { Success = true };
        
        try
        {
            // 계획에서 액션 추출
            var actions = ExtractActionsFromPlan(plan.Content);
            
            _logger.LogDebug("[PlanExecute] 실행할 액션 수: {Count}", actions.Count);
            
            foreach (var action in actions)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var stepResult = await ExecuteActionAsync(context, action, cancellationToken);
                if (!stepResult.Success)
                {
                    result.Success = false;
                    result.ErrorMessage = stepResult.ErrorMessage;
                    _logger.LogWarning("[PlanExecute] 액션 실행 실패: {Action}", action.Type);
                    // 실패해도 다음 액션 시도
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PlanExecute] 계획 실행 중 오류");
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }
        
        return result;
    }

    private List<PlannedAction> ExtractActionsFromPlan(string planContent)
    {
        var actions = new List<PlannedAction>();
        
        try
        {
            // JSON 형식으로 계획이 반환된다고 가정
            using var doc = JsonDocument.Parse(planContent);
            var root = doc.RootElement;
            
            if (root.TryGetProperty("actions", out var actionsElement))
            {
                foreach (var actionElement in actionsElement.EnumerateArray())
                {
                    var action = new PlannedAction
                    {
                        Type = actionElement.GetProperty("type").GetString() ?? "unknown",
                        Name = actionElement.GetProperty("name").GetString() ?? "",
                        Parameters = new Dictionary<string, object>()
                    };
                    
                    if (actionElement.TryGetProperty("parameters", out var paramsElement))
                    {
                        foreach (var param in paramsElement.EnumerateObject())
                        {
                            action.Parameters[param.Name] = param.Value.ToString();
                        }
                    }
                    
                    actions.Add(action);
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "[PlanExecute] 계획 파싱 실패, 빈 액션 목록 반환");
        }
        
        return actions;
    }

    private async Task<ExecutionResult> ExecuteActionAsync(
        OrchestrationContext context,
        PlannedAction action,
        CancellationToken cancellationToken)
    {
        var step = new ExecutionStep
        {
            StepType = action.Type,
            Description = $"{action.Type}: {action.Name}",
            Input = JsonSerializer.Serialize(action.Parameters),
            StartTime = DateTime.UtcNow
        };
        
        try
        {
            if (action.Type.StartsWith("llm_", StringComparison.OrdinalIgnoreCase))
            {
                await ExecuteLLMActionAsync(context, action, step, cancellationToken);
            }
            else if (action.Type.StartsWith("tool_", StringComparison.OrdinalIgnoreCase))
            {
                await ExecuteToolActionAsync(context, action, step, cancellationToken);
            }
            else
            {
                _logger.LogWarning("[PlanExecute] 알 수 없는 액션 타입: {Type}", action.Type);
                step.Success = false;
                step.ErrorMessage = $"알 수 없는 액션 타입: {action.Type}";
            }
            
            step.Success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PlanExecute] 액션 실행 오류: {ActionName}", action.Name);
            step.Success = false;
            step.ErrorMessage = ex.Message;
        }
        finally
        {
            step.EndTime = DateTime.UtcNow;
            context.AddExecutionStep(step);
        }
        
        return new ExecutionResult 
        { 
            Success = step.Success,
            ErrorMessage = step.ErrorMessage 
        };
    }

    private async Task ExecuteLLMActionAsync(
        OrchestrationContext context,
        PlannedAction action,
        ExecutionStep step,
        CancellationToken cancellationToken)
    {
        var functionName = action.Name.Replace("llm_", "");
        var llmFunction = _registry.GetLLMFunction(functionName);
        
        if (llmFunction == null)
        {
            throw new InvalidOperationException($"LLM 기능을 찾을 수 없습니다: {functionName}");
        }
        
        var llmContext = new LLMContext
        {
            UserRequest = context.UserRequest,
            ExecutionHistory = context.ExecutionHistory,
            SharedData = context.SharedData,
            Parameters = action.Parameters
        };
        
        var result = await llmFunction.ExecuteAsync(llmContext, cancellationToken);
        step.Output = result.Content;
        step.ExecutionTime = result.ExecutionTime;
        
        // 결과를 SharedData에 저장
        context.SharedData[$"llm_{functionName}_result"] = result.Content;
    }

    private async Task ExecuteToolActionAsync(
        OrchestrationContext context,
        PlannedAction action,
        ExecutionStep step,
        CancellationToken cancellationToken)
    {
        var toolName = action.Name.Replace("tool_", "");
        var tool = _registry.GetTool(toolName);
        
        if (tool == null)
        {
            throw new InvalidOperationException($"도구를 찾을 수 없습니다: {toolName}");
        }
        
        var toolInput = new ToolInput
        {
            Parameters = action.Parameters
        };
        
        var result = await tool.ExecuteAsync(toolInput, cancellationToken);
        step.Output = JsonSerializer.Serialize(result.Data);
        step.ExecutionTime = result.ExecutionTime;
        
        // 결과를 SharedData에 저장
        context.SharedData[$"tool_{toolName}_result"] = result.Data;
    }

    private async Task<bool> CheckCompletionAsync(
        OrchestrationContext context,
        CancellationToken cancellationToken)
    {
        var completionChecker = _registry.GetLLMFunction("completion_checker");
        if (completionChecker == null)
        {
            _logger.LogWarning("[PlanExecute] Completion checker를 찾을 수 없음, 기본 로직 사용");
            return context.ExecutionHistory.Count(s => s.IsSuccess) >= 3; // 기본: 3개 이상 성공
        }
        
        var llmContext = new LLMContext
        {
            UserRequest = context.UserRequest,
            ExecutionHistory = context.ExecutionHistory,
            SharedData = context.SharedData
        };
        
        llmContext.Parameters["user_request"] = context.UserRequest;
        llmContext.Parameters["execution_history"] = JsonSerializer.Serialize(context.ExecutionHistory);
        
        var result = await completionChecker.ExecuteAsync(llmContext, cancellationToken);
        
        try
        {
            using var doc = JsonDocument.Parse(result.Content);
            return doc.RootElement.GetProperty("is_complete").GetBoolean();
        }
        catch
        {
            _logger.LogWarning("[PlanExecute] 완료 여부 파싱 실패");
            return false;
        }
    }
    
    private class PlannedAction
    {
        public string Type { get; set; } = "";
        public string Name { get; set; } = "";
        public Dictionary<string, object> Parameters { get; set; } = new();
    }
    
    private class ExecutionResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }
}