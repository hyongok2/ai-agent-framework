using AIAgentFramework.Core.Interfaces;
using AIAgentFramework.Core.Models;

namespace AIAgentFramework.Core.Actions;

/// <summary>
/// 도구 실행 액션
/// </summary>
public class ToolAction : OrchestrationActionBase
{
    /// <inheritdoc />
    public override ActionType Type => ActionType.Tool;
    
    /// <inheritdoc />
    public override string Name { get; }
    
    /// <summary>
    /// 실행할 도구 이름
    /// </summary>
    public string ToolName { get; }
    
    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="toolName">도구 이름</param>
    /// <param name="parameters">매개변수</param>
    public ToolAction(string toolName, Dictionary<string, object>? parameters = null) 
        : base(parameters)
    {
        if (string.IsNullOrWhiteSpace(toolName))
            throw new ArgumentException("Tool name cannot be null or empty", nameof(toolName));
            
        ToolName = toolName;
        Name = $"Tool:{toolName}";
    }
    
    /// <inheritdoc />
    public override async Task<ActionResult> ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            var tool = context.Registry.GetTool(ToolName);
            if (tool == null)
            {
                return ActionResult.Failure(
                    $"Tool '{ToolName}' not found in registry",
                    DateTime.UtcNow - startTime);
            }
            
            var toolInput = new ToolInput();
            
            // 매개변수를 도구 입력에 추가
            foreach (var param in Parameters)
            {
                toolInput.Parameters[param.Key] = param.Value;
            }
            
            var result = await tool.ExecuteAsync(toolInput, cancellationToken);
            
            return ActionResult.Success(result, DateTime.UtcNow - startTime);
        }
        catch (Exception ex)
        {
            return ActionResult.Failure(
                $"Tool execution failed: {ex.Message}",
                DateTime.UtcNow - startTime);
        }
    }
}