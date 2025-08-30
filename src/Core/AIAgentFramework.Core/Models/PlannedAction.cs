using AIAgentFramework.Core.Interfaces;

namespace AIAgentFramework.Core.Models;

/// <summary>
/// 계획된 액션 구현
/// </summary>
public class PlannedAction : IPlannedAction
{
    /// <inheritdoc />
    public string Type { get; set; } = string.Empty;
    
    /// <inheritdoc />
    public string Name { get; set; } = string.Empty;
    
    /// <inheritdoc />
    public Dictionary<string, object> Parameters { get; set; } = new();
    
    /// <inheritdoc />
    public int Order { get; set; }
}