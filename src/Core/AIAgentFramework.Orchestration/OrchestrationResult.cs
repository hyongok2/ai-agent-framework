using AIAgentFramework.Core.Interfaces;

namespace AIAgentFramework.Orchestration;

/// <summary>
/// 오케스트레이션 결과 구현
/// </summary>
public class OrchestrationResult : IOrchestrationResult
{
    /// <inheritdoc />
    public string SessionId => Context.SessionId;
    
    /// <inheritdoc />
    public bool IsCompleted => Context.IsCompleted;
    
    /// <inheritdoc />
    public IOrchestrationContext Context { get; }

    /// <inheritdoc />
    public bool IsSuccess => Context.IsCompleted && string.IsNullOrEmpty(GetErrorMessage());

    /// <inheritdoc />
    public string? ErrorMessage => GetErrorMessage();
    
    /// <inheritdoc />
    public string FinalResponse => GetFinalResponse();
    
    /// <inheritdoc />
    public List<IExecutionStep> ExecutionSteps => Context.ExecutionHistory;

    /// <inheritdoc />
    public TimeSpan ExecutionTime => GetTotalExecutionTime();
    
    /// <inheritdoc />
    public TimeSpan TotalDuration => GetTotalExecutionTime();

    /// <inheritdoc />
    public Dictionary<string, object> Metadata { get; }

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="context">오케스트레이션 컨텍스트</param>
    public OrchestrationResult(IOrchestrationContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Metadata = new Dictionary<string, object>
        {
            ["session_id"] = context.SessionId,
            ["step_count"] = context.ExecutionHistory.Count,
            ["successful_steps"] = GetSuccessfulStepCount(),
            ["failed_steps"] = GetFailedStepCount(),
            ["started_at"] = GetStartTime(),
            ["completed_at"] = GetEndTime() ?? (object)"Not completed"
        };
    }
    
    private string GetFinalResponse()
    {
        var lastStep = Context.ExecutionHistory.LastOrDefault();
        return lastStep?.Output?.ToString() ?? "No response available";
    }
    
    private string? GetErrorMessage()
    {
        return (Context as OrchestrationContext)?.ErrorMessage;
    }
    
    private TimeSpan GetTotalExecutionTime()
    {
        return (Context as OrchestrationContext)?.TotalExecutionTime ?? TimeSpan.Zero;
    }
    
    private int GetSuccessfulStepCount()
    {
        return Context.ExecutionHistory.Count(s => (s as ExecutionStep)?.Success == true);
    }
    
    private int GetFailedStepCount()
    {
        return Context.ExecutionHistory.Count(s => (s as ExecutionStep)?.Success == false);
    }
    
    private DateTime GetStartTime()
    {
        return (Context as OrchestrationContext)?.StartTime ?? DateTime.UtcNow;
    }
    
    private DateTime? GetEndTime()
    {
        return (Context as OrchestrationContext)?.EndTime;
    }
}