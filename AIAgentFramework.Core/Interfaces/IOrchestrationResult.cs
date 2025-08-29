namespace AIAgentFramework.Core.Interfaces;

/// <summary>
/// 오케스트레이션 실행 결과
/// </summary>
public interface IOrchestrationResult
{
    /// <summary>
    /// 세션 ID
    /// </summary>
    string SessionId { get; }
    
    /// <summary>
    /// 성공 여부
    /// </summary>
    bool IsSuccess { get; }
    
    /// <summary>
    /// 완료 여부
    /// </summary>
    bool IsCompleted { get; }
    
    /// <summary>
    /// 최종 응답
    /// </summary>
    string? FinalResponse { get; }
    
    /// <summary>
    /// 실행 단계들
    /// </summary>
    List<IExecutionStep> ExecutionSteps { get; }
    
    /// <summary>
    /// 총 실행 시간
    /// </summary>
    TimeSpan TotalDuration { get; }
    
    /// <summary>
    /// 메타데이터
    /// </summary>
    Dictionary<string, object> Metadata { get; }
    
    /// <summary>
    /// 오류 메시지 (실패 시)
    /// </summary>
    string? ErrorMessage { get; }
}