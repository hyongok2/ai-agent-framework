using AIAgentFramework.Core.Interfaces;

namespace AIAgentFramework.Core.Models;

/// <summary>
/// 오케스트레이션 컨텍스트 구현
/// </summary>
public class OrchestrationContext : IOrchestrationContext
{
    /// <inheritdoc />
    public string SessionId { get; set; } = Guid.NewGuid().ToString();
    
    /// <inheritdoc />
    public IUserRequest OriginalRequest { get; set; } = null!;
    
    /// <inheritdoc />
    public List<IExecutionStep> ExecutionHistory { get; set; } = new();
    
    /// <inheritdoc />
    public Dictionary<string, object> SharedData { get; set; } = new();
    
    /// <inheritdoc />
    public bool IsCompleted { get; set; }
    
    /// <inheritdoc />
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    
    /// <inheritdoc />
    public DateTime? CompletedAt { get; set; }
}