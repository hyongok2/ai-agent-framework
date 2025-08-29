using AIAgentFramework.Core.Interfaces;

namespace AIAgentFramework.Core.Models;

/// <summary>
/// 오케스트레이션 결과 구현
/// </summary>
public class OrchestrationResult : IOrchestrationResult
{
    /// <inheritdoc />
    public string SessionId { get; set; } = string.Empty;
    
    /// <inheritdoc />
    public bool IsSuccess { get; set; }
    
    /// <inheritdoc />
    public bool IsCompleted { get; set; }
    
    /// <inheritdoc />
    public string? FinalResponse { get; set; }
    
    /// <inheritdoc />
    public List<IExecutionStep> ExecutionSteps { get; set; } = new();
    
    /// <inheritdoc />
    public TimeSpan TotalDuration { get; set; }
    
    /// <inheritdoc />
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    /// <inheritdoc />
    public string? ErrorMessage { get; set; }
}