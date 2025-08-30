using AIAgentFramework.Core.Actions;
using AIAgentFramework.Core.Interfaces;
using AIAgentFramework.Core.Models;

namespace AIAgentFramework.Core.Factories;

/// <summary>
/// 타입 안전한 액션 생성을 위한 팩토리
/// </summary>
public class ActionFactory : IActionFactory
{
    /// <summary>
    /// LLM 액션 생성
    /// </summary>
    /// <param name="functionName">LLM 기능 이름</param>
    /// <param name="parameters">매개변수</param>
    /// <returns>LLM 액션</returns>
    public IOrchestrationAction CreateLLMAction(string functionName, Dictionary<string, object>? parameters = null)
    {
        return new LLMAction(functionName, parameters);
    }
    
    /// <summary>
    /// 도구 액션 생성
    /// </summary>
    /// <param name="toolName">도구 이름</param>
    /// <param name="parameters">매개변수</param>
    /// <returns>도구 액션</returns>
    public IOrchestrationAction CreateToolAction(string toolName, Dictionary<string, object>? parameters = null)
    {
        return new ToolAction(toolName, parameters);
    }
    
    /// <summary>
    /// JSON 객체에서 액션 생성
    /// </summary>
    /// <param name="actionData">액션 데이터 (JSON 파싱 결과)</param>
    /// <returns>생성된 액션</returns>
    /// <exception cref="ArgumentException">잘못된 액션 데이터</exception>
    public IOrchestrationAction CreateFromJsonObject(Dictionary<string, object> actionData)
    {
        if (!actionData.TryGetValue("type", out var typeValue))
        {
            throw new ArgumentException("Action type is required");
        }
        
        var typeString = typeValue.ToString();
        if (string.IsNullOrEmpty(typeString))
        {
            throw new ArgumentException("Action type cannot be empty");
        }
        
        // 매개변수 추출
        var parameters = new Dictionary<string, object>();
        if (actionData.TryGetValue("parameters", out var parametersValue) && 
            parametersValue is Dictionary<string, object> paramDict)
        {
            parameters = paramDict;
        }
        
        return typeString.ToLowerInvariant() switch
        {
            "llm" or "llm_function" => CreateLLMActionFromData(actionData, parameters),
            "tool" => CreateToolActionFromData(actionData, parameters),
            _ => throw new ArgumentException($"Unsupported action type: {typeString}")
        };
    }
    
    /// <summary>
    /// 여러 액션을 일괄 생성
    /// </summary>
    /// <param name="actionsData">액션 데이터 배열</param>
    /// <returns>생성된 액션 목록</returns>
    public List<IOrchestrationAction> CreateActionsFromJsonArray(List<Dictionary<string, object>> actionsData)
    {
        var actions = new List<IOrchestrationAction>();
        
        foreach (var actionData in actionsData)
        {
            try
            {
                var action = CreateFromJsonObject(actionData);
                actions.Add(action);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Failed to create action from data: {ex.Message}", ex);
            }
        }
        
        return actions;
    }
    
    private IOrchestrationAction CreateLLMActionFromData(Dictionary<string, object> actionData, Dictionary<string, object> parameters)
    {
        if (!actionData.TryGetValue("function_name", out var functionNameValue) &&
            !actionData.TryGetValue("name", out functionNameValue))
        {
            throw new ArgumentException("LLM action requires 'function_name' or 'name'");
        }
        
        var functionName = functionNameValue.ToString();
        if (string.IsNullOrEmpty(functionName))
        {
            throw new ArgumentException("LLM function name cannot be empty");
        }
        
        return CreateLLMAction(functionName, parameters);
    }
    
    private IOrchestrationAction CreateToolActionFromData(Dictionary<string, object> actionData, Dictionary<string, object> parameters)
    {
        if (!actionData.TryGetValue("tool_name", out var toolNameValue) &&
            !actionData.TryGetValue("name", out toolNameValue))
        {
            throw new ArgumentException("Tool action requires 'tool_name' or 'name'");
        }
        
        var toolName = toolNameValue.ToString();
        if (string.IsNullOrEmpty(toolName))
        {
            throw new ArgumentException("Tool name cannot be empty");
        }
        
        return CreateToolAction(toolName, parameters);
    }
}