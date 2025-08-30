using AIAgentFramework.Core.Interfaces;
using AIAgentFramework.Core.Models;

namespace AIAgentFramework.Core.Actions;

/// <summary>
/// 오케스트레이션 액션 기본 클래스
/// </summary>
public abstract class OrchestrationActionBase : IOrchestrationAction
{
    /// <inheritdoc />
    public abstract ActionType Type { get; }
    
    /// <inheritdoc />
    public abstract string Name { get; }
    
    /// <inheritdoc />
    public Dictionary<string, object> Parameters { get; } = new();
    
    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="parameters">액션 매개변수</param>
    protected OrchestrationActionBase(Dictionary<string, object>? parameters = null)
    {
        if (parameters != null)
        {
            Parameters = new Dictionary<string, object>(parameters);
        }
    }
    
    /// <inheritdoc />
    public abstract Task<ActionResult> ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 매개변수 값 가져오기
    /// </summary>
    protected T? GetParameter<T>(string key, T? defaultValue = default)
    {
        if (Parameters.TryGetValue(key, out var value))
        {
            try
            {
                return (T?)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }
    
    /// <summary>
    /// 필수 매개변수 값 가져오기
    /// </summary>
    protected T GetRequiredParameter<T>(string key)
    {
        if (!Parameters.TryGetValue(key, out var value))
        {
            throw new ArgumentException($"Required parameter '{key}' is missing");
        }
        
        try
        {
            return (T)Convert.ChangeType(value, typeof(T))!;
        }
        catch
        {
            throw new ArgumentException($"Parameter '{key}' cannot be converted to type {typeof(T).Name}");
        }
    }
}