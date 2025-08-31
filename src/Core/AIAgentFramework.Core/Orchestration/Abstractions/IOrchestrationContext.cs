using AIAgentFramework.Core.Orchestration.Execution;
using AIAgentFramework.Core.User;

namespace AIAgentFramework.Core.Orchestration.Abstractions;

/// <summary>
/// 오케스트레이션 실행 컨텍스트
/// </summary>
public interface IOrchestrationContext
{
    /// <summary>
    /// 세션 ID
    /// </summary>
    string SessionId { get; }
    
    /// <summary>
    /// 원본 사용자 요청
    /// </summary>
    IUserRequest OriginalRequest { get; }
    
    /// <summary>
    /// 사용자 요청 텍스트
    /// </summary>
    string UserRequest { get; }
    
    /// <summary>
    /// 실행 이력
    /// </summary>
    List<IExecutionStep> ExecutionHistory { get; }
    
    /// <summary>
    /// 공유 데이터
    /// </summary>
    Dictionary<string, object> SharedData { get; }
    
    /// <summary>
    /// 완료 여부
    /// </summary>
    bool IsCompleted { get; set; }
    
    /// <summary>
    /// 시작 시간
    /// </summary>
    DateTime StartedAt { get; }
    
    /// <summary>
    /// 완료 시간
    /// </summary>
    DateTime? CompletedAt { get; set; }
    
    /// <summary>
    /// 실행 단계 추가
    /// </summary>
    /// <param name="step">실행 단계</param>
    void AddExecutionStep(IExecutionStep step);
}