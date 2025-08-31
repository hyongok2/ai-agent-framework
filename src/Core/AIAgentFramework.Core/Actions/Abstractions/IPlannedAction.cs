namespace AIAgentFramework.Core.Actions.Abstractions;

/// <summary>
/// 계획된 액션
/// </summary>
public interface IPlannedAction
{
    /// <summary>
    /// 액션 유형 (LLM 또는 TOOL)
    /// </summary>
    string Type { get; }
    
    /// <summary>
    /// 기능 이름
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// 파라미터
    /// </summary>
    Dictionary<string, object> Parameters { get; }
    
    /// <summary>
    /// 실행 순서
    /// </summary>
    int Order { get; }
}