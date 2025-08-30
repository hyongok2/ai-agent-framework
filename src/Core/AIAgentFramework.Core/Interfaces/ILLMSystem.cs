namespace AIAgentFramework.Core.Interfaces;

/// <summary>
/// LLM 시스템 인터페이스
/// </summary>
public interface ILLMSystem
{
    /// <summary>
    /// Planner 기능을 실행합니다.
    /// </summary>
    /// <param name="context">오케스트레이션 컨텍스트</param>
    /// <returns>LLM 실행 결과</returns>
    Task<ILLMResult> ExecutePlannerAsync(IOrchestrationContext context);
    
    /// <summary>
    /// 지정된 LLM 기능을 실행합니다.
    /// </summary>
    /// <param name="role">역할 이름</param>
    /// <param name="context">LLM 컨텍스트</param>
    /// <returns>LLM 실행 결과</returns>
    Task<ILLMResult> ExecuteFunctionAsync(string role, ILLMContext context);
    
    /// <summary>
    /// 계획된 액션을 실행합니다.
    /// </summary>
    /// <param name="action">계획된 액션</param>
    /// <returns>LLM 실행 결과</returns>
    Task<ILLMResult> ExecuteFunctionAsync(IPlannedAction action);
    
    /// <summary>
    /// 사용 가능한 모든 LLM 기능 목록을 조회합니다.
    /// </summary>
    /// <returns>LLM 기능 목록</returns>
    List<ILLMFunction> GetAvailableFunctions();
}