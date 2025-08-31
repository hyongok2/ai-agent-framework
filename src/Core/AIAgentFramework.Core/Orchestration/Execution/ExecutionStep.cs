namespace AIAgentFramework.Core.Orchestration.Execution;

/// <summary>
/// 실행 단계 구현
/// </summary>
public class ExecutionStep : IExecutionStep
{
    /// <inheritdoc />
    public string StepId { get; set; } = Guid.NewGuid().ToString();
    
    /// <inheritdoc />
    public string StepType { get; set; } = string.Empty;
    
    /// <inheritdoc />
    public string FunctionName { get; set; } = string.Empty;
    
    /// <inheritdoc />
    public string Description { get; set; } = string.Empty;
    
    /// <inheritdoc />
    public object Input { get; set; } = new();
    
    /// <inheritdoc />
    public object Output { get; set; } = new();
    
    /// <inheritdoc />
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    
    /// <inheritdoc />
    public TimeSpan Duration { get; set; }
    
    /// <inheritdoc />
    public bool IsSuccess { get; set; }
    
    /// <inheritdoc />
    public string? ErrorMessage { get; set; }
}