using AIAgentFramework.Core.Interfaces;
using AIAgentFramework.Core.Models;

namespace AIAgentFramework.Core.Actions;

/// <summary>
/// LLM 기능 실행 액션
/// </summary>
public class LLMAction : OrchestrationActionBase
{
    /// <inheritdoc />
    public override ActionType Type => ActionType.LLMFunction;
    
    /// <inheritdoc />
    public override string Name { get; }
    
    /// <summary>
    /// 실행할 LLM 기능 이름
    /// </summary>
    public string FunctionName { get; }
    
    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="functionName">LLM 기능 이름</param>
    /// <param name="parameters">매개변수</param>
    public LLMAction(string functionName, Dictionary<string, object>? parameters = null) 
        : base(parameters)
    {
        if (string.IsNullOrWhiteSpace(functionName))
            throw new ArgumentException("Function name cannot be null or empty", nameof(functionName));
            
        FunctionName = functionName;
        Name = $"LLM:{functionName}";
    }
    
    /// <inheritdoc />
    public override async Task<ActionResult> ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            var llmFunction = context.Registry.GetLLMFunction(FunctionName);
            if (llmFunction == null)
            {
                return ActionResult.Failure(
                    $"LLM function '{FunctionName}' not found in registry",
                    DateTime.UtcNow - startTime);
            }
            
            var llmContext = new LLMContext
            {
                UserRequest = context.UserRequest,
                ExecutionHistory = context.ExecutionHistory,
                SharedData = context.SharedData
            };
            
            // 매개변수를 LLM 컨텍스트에 추가
            foreach (var param in Parameters)
            {
                llmContext.Parameters[param.Key] = param.Value;
            }
            
            var result = await llmFunction.ExecuteAsync(llmContext, cancellationToken);
            
            return ActionResult.Success(result, DateTime.UtcNow - startTime);
        }
        catch (Exception ex)
        {
            return ActionResult.Failure(
                $"LLM function execution failed: {ex.Message}",
                DateTime.UtcNow - startTime);
        }
    }
}