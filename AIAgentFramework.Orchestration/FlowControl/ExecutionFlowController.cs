using AIAgentFramework.Core.Interfaces;
using AIAgentFramework.Core.Models;
using Microsoft.Extensions.Logging;

namespace AIAgentFramework.Orchestration.FlowControl;

public class ExecutionFlowController
{
    private readonly IRegistry _registry;
    private readonly ILogger<ExecutionFlowController> _logger;

    public ExecutionFlowController(IRegistry registry, ILogger<ExecutionFlowController> logger)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PlanResult> ExecutePlanAsync(OrchestrationContext context, CancellationToken cancellationToken = default)
    {
        var planner = _registry.GetLLMFunction("planner");
        if (planner == null)
            return PlanResult.CreateFailure("Planner 기능을 찾을 수 없습니다.");

        var llmContext = new LLMContext
        {
            UserRequest = context.UserRequest,
            ExecutionHistory = context.ExecutionHistory,
            SharedData = context.SharedData
        };
        llmContext.Parameters["user_request"] = context.UserRequest;

        var result = await planner.ExecuteAsync(llmContext, cancellationToken);
        
        if (!result.Success)
            return PlanResult.CreateFailure(result.ErrorMessage ?? "계획 수립 실패");

        if (result.Data.ContainsKey("plan_actions"))
        {
            context.SharedData["plan_actions"] = result.Data["plan_actions"];
        }

        return PlanResult.CreateSuccess(result.Data);
    }

    public async Task ExecutePlannedActionsAsync(OrchestrationContext context, CancellationToken cancellationToken = default)
    {
        if (!context.SharedData.ContainsKey("plan_actions"))
            return;

        var actions = context.SharedData["plan_actions"] as List<object>;
        if (actions == null) return;

        foreach (var action in actions)
        {
            if (cancellationToken.IsCancellationRequested) break;
            await ExecuteSingleActionAsync(context, action, cancellationToken);
        }
    }

    private async Task ExecuteSingleActionAsync(OrchestrationContext context, object action, CancellationToken cancellationToken)
    {
        var actionType = DetermineActionType(action);
        
        switch (actionType)
        {
            case ActionType.LLMFunction:
                await ExecuteLLMFunctionAsync(context, action, cancellationToken);
                break;
            case ActionType.Tool:
                await ExecuteToolAsync(context, action, cancellationToken);
                break;
        }
    }

    private async Task ExecuteLLMFunctionAsync(OrchestrationContext context, object action, CancellationToken cancellationToken)
    {
        var functionName = ExtractFunctionName(action);
        var llmFunction = _registry.GetLLMFunction(functionName);
        
        if (llmFunction == null) return;

        var llmContext = new LLMContext
        {
            ExecutionHistory = context.ExecutionHistory,
            SharedData = context.SharedData
        };

        var result = await llmFunction.ExecuteAsync(llmContext, cancellationToken);
        
        var step = new ExecutionStep("LLM", $"LLM 기능 실행: {functionName}")
            .WithInput(action.ToString() ?? "")
            .WithOutput(result.Content);

        if (result.Success)
            step.MarkAsSuccess();
        else
            step.MarkAsFailure(result.ErrorMessage ?? "실행 실패");

        context.AddExecutionStep(step);
    }

    private async Task ExecuteToolAsync(OrchestrationContext context, object action, CancellationToken cancellationToken)
    {
        var toolName = ExtractToolName(action);
        var tool = _registry.GetTool(toolName);
        
        if (tool == null) return;

        var toolInput = new ToolInput();
        var result = await tool.ExecuteAsync(toolInput, cancellationToken);
        
        var step = new ExecutionStep("Tool", $"도구 실행: {toolName}")
            .WithInput(action.ToString() ?? "")
            .WithOutput(result.Data?.ToString() ?? "");

        if (result.Success)
            step.MarkAsSuccess();
        else
            step.MarkAsFailure(result.ErrorMessage ?? "실행 실패");

        context.AddExecutionStep(step);
    }

    public bool CheckCompletion(OrchestrationContext context)
    {
        if (context.SharedData.ContainsKey("is_completed") && 
            context.SharedData["is_completed"] is bool isCompleted && isCompleted)
        {
            return true;
        }

        if (context.SharedData.ContainsKey("plan_actions") && 
            context.SharedData["plan_actions"] is List<object> actions)
        {
            var completedActions = context.ExecutionHistory.Count(s => s.IsSuccess);
            return completedActions >= actions.Count;
        }

        return context.ExecutionHistory.Count >= 10;
    }

    private ActionType DetermineActionType(object action)
    {
        var actionStr = action.ToString() ?? "";
        
        if (actionStr.StartsWith("llm_") || actionStr.Contains("function"))
            return ActionType.LLMFunction;
        
        if (actionStr.StartsWith("tool_") || actionStr.Contains("tool"))
            return ActionType.Tool;
        
        return ActionType.Unknown;
    }

    private string ExtractFunctionName(object action)
    {
        var actionStr = action.ToString() ?? "";
        return actionStr.Replace("llm_", "").Replace("function_", "");
    }

    private string ExtractToolName(object action)
    {
        var actionStr = action.ToString() ?? "";
        return actionStr.Replace("tool_", "");
    }
}

public enum ActionType
{
    Unknown,
    LLMFunction,
    Tool
}

public class PlanResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();

    public static PlanResult CreateSuccess(Dictionary<string, object> data) => new() { Success = true, Data = data };
    public static PlanResult CreateFailure(string error) => new() { Success = false, ErrorMessage = error };
}