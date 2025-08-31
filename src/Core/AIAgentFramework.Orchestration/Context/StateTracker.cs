

using AIAgentFramework.Core.Orchestration.Abstractions;

namespace AIAgentFramework.Orchestration.Context;

public class StateTracker
{
    public ExecutionState GetCurrentState(IOrchestrationContext context)
    {
        if (context.IsCompleted)
        {
            var errorMessage = (context as OrchestrationContext)?.ErrorMessage;
        return string.IsNullOrEmpty(errorMessage) ? 
                ExecutionState.Completed : ExecutionState.Failed;
        }

        if (!context.ExecutionHistory.Any())
            return ExecutionState.NotStarted;

        var lastStep = context.ExecutionHistory.LastOrDefault();
        if (lastStep != null && !lastStep.IsSuccess)
            return ExecutionState.Error;

        return ExecutionState.InProgress;
    }

    public ExecutionMetrics CalculateMetrics(IOrchestrationContext context)
    {
        var steps = context.ExecutionHistory;
        var successfulSteps = steps.Count(s => s.IsSuccess);
        var failedSteps = steps.Count(s => !s.IsSuccess);
        
        var totalDuration = context.CompletedAt?.Subtract(context.StartedAt) ?? 
                           DateTime.UtcNow.Subtract(context.StartedAt);

        return new ExecutionMetrics
        {
            TotalSteps = steps.Count,
            SuccessfulSteps = successfulSteps,
            FailedSteps = failedSteps,
            SuccessRate = steps.Count > 0 ? (double)successfulSteps / steps.Count : 0,
            TotalDuration = totalDuration,
            AverageStepDuration = steps.Count > 0 ? 
                TimeSpan.FromMilliseconds(steps.Average(s => s.Duration.TotalMilliseconds)) : 
                TimeSpan.Zero
        };
    }

    public List<string> GetExecutionSummary(IOrchestrationContext context)
    {
        var summary = new List<string>();
        
        foreach (var step in context.ExecutionHistory)
        {
            var status = step.IsSuccess ? "✓" : "✗";
            var duration = $"{step.Duration.TotalMilliseconds:F0}ms";
            summary.Add($"{status} [{step.StepType}] {step.Description} ({duration})");
        }

        return summary;
    }
}

public enum ExecutionState
{
    NotStarted,
    InProgress,
    Completed,
    Failed,
    Error
}

public class ExecutionMetrics
{
    public int TotalSteps { get; set; }
    public int SuccessfulSteps { get; set; }
    public int FailedSteps { get; set; }
    public double SuccessRate { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public TimeSpan AverageStepDuration { get; set; }
}