namespace AIAgentFramework.Core.Interfaces;

/// <summary>
/// 오케스트레이션 엔진의 핵심 인터페이스
/// </summary>
public interface IOrchestrationEngine
{
    /// <summary>
    /// 사용자 요청을 실행합니다.
    /// </summary>
    /// <param name="request">사용자 요청</param>
    /// <returns>오케스트레이션 결과</returns>
    Task<IOrchestrationResult> ExecuteAsync(IUserRequest request);
    
    /// <summary>
    /// 기존 컨텍스트를 계속 실행합니다.
    /// </summary>
    /// <param name="context">오케스트레이션 컨텍스트</param>
    /// <returns>오케스트레이션 결과</returns>
    Task<IOrchestrationResult> ContinueAsync(IOrchestrationContext context);
}