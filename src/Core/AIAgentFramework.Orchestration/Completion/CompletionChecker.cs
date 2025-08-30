using AIAgentFramework.Core.Interfaces;

namespace AIAgentFramework.Orchestration.Completion;

public class CompletionChecker
{
    public bool IsCompleted(IOrchestrationContext context)
    {
        if (context.SharedData.ContainsKey("is_completed") && 
            context.SharedData["is_completed"] is bool isCompleted && isCompleted)
        {
            return true;
        }

        if (context.ExecutionHistory.Count >= GetMaxSteps(context))
        {
            return true;
        }

        if (AllPlannedActionsCompleted(context))
        {
            return true;
        }

        if (RequiresUserResponse(context))
        {
            return true;
        }

        if (HasCriticalError(context))
        {
            return true;
        }

        return false;
    }

    private int GetMaxSteps(IOrchestrationContext context)
    {
        return context.SharedData.ContainsKey("max_steps") ? 
            (int)context.SharedData["max_steps"] : 20;
    }

    private bool AllPlannedActionsCompleted(IOrchestrationContext context)
    {
        if (!context.SharedData.ContainsKey("plan_actions"))
            return false;

        var actions = context.SharedData["plan_actions"] as List<object>;
        if (actions == null) return false;

        var completedActions = context.ExecutionHistory.Count(s => s.IsSuccess);
        return completedActions >= actions.Count;
    }

    private bool RequiresUserResponse(IOrchestrationContext context)
    {
        var lastStep = context.ExecutionHistory.LastOrDefault();
        if (lastStep?.Output is string output)
        {
            return output.Contains("user_response_required") || 
                   output.Contains("clarification_needed");
        }
        return false;
    }

    private bool HasCriticalError(IOrchestrationContext context)
    {
        var recentErrors = context.ExecutionHistory
            .TakeLast(3)
            .Count(s => !s.IsSuccess);
        
        return recentErrors >= 2;
    }
}