using AIAgentFramework.Core.Actions.Abstractions;

namespace AIAgentFramework.Core.Tools.Abstractions;


/// <summary>
/// 도구 시스템 인터페이스
/// </summary>
public interface IToolSystem
{
    /// <summary>
    /// 도구를 실행합니다.
    /// </summary>
    /// <param name="toolName">도구 이름</param>
    /// <param name="input">입력 데이터</param>
    /// <returns>실행 결과</returns>
    Task<IToolResult> ExecuteToolAsync(string toolName, IToolInput input);
    
    /// <summary>
    /// 계획된 액션을 실행합니다.
    /// </summary>
    /// <param name="action">계획된 액션</param>
    /// <returns>실행 결과</returns>
    Task<IToolResult> ExecuteToolAsync(IPlannedAction action);
    
    /// <summary>
    /// 여러 도구를 병렬로 실행합니다.
    /// </summary>
    /// <param name="tools">실행할 도구들</param>
    /// <returns>실행 결과 목록</returns>
    Task<List<IToolResult>> ExecuteToolsAsync(IEnumerable<(string toolName, IToolInput input)> tools);
    
    /// <summary>
    /// 사용 가능한 모든 도구 목록을 조회합니다.
    /// </summary>
    /// <returns>도구 목록</returns>
    List<ITool> GetAvailableTools();
    
    /// <summary>
    /// 도구가 존재하는지 확인합니다.
    /// </summary>
    /// <param name="toolName">도구 이름</param>
    /// <returns>존재 여부</returns>
    bool ToolExists(string toolName);
}