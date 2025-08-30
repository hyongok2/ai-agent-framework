namespace AIAgentFramework.Core.Interfaces;

/// <summary>
/// 오케스트레이션 옵저버 인터페이스
/// </summary>
public interface IOrchestrationObserver
{
    /// <summary>
    /// 실행 시작 이벤트
    /// </summary>
    /// <param name="context">오케스트레이션 컨텍스트</param>
    /// <returns>처리 작업</returns>
    Task OnExecutionStartedAsync(IOrchestrationContext context);
    
    /// <summary>
    /// 단계 완료 이벤트
    /// </summary>
    /// <param name="step">실행 단계</param>
    /// <returns>처리 작업</returns>
    Task OnStepCompletedAsync(IExecutionStep step);
    
    /// <summary>
    /// 실행 완료 이벤트
    /// </summary>
    /// <param name="result">오케스트레이션 결과</param>
    /// <returns>처리 작업</returns>
    Task OnExecutionCompletedAsync(IOrchestrationResult result);
    
    /// <summary>
    /// 오류 발생 이벤트
    /// </summary>
    /// <param name="exception">발생한 예외</param>
    /// <param name="context">오케스트레이션 컨텍스트</param>
    /// <returns>처리 작업</returns>
    Task OnErrorOccurredAsync(Exception exception, IOrchestrationContext context);
}