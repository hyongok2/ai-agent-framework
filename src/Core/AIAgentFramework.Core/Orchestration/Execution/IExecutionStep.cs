namespace AIAgentFramework.Core.Orchestration.Execution;


/// <summary>
/// 실행 단계 인터페이스
/// </summary>
public interface IExecutionStep
{
    /// <summary>
    /// 단계 ID
    /// </summary>
    string StepId { get; }
    
    /// <summary>
    /// 단계 타입
    /// </summary>
    string StepType { get; }
    
    /// <summary>
    /// 기능 이름
    /// </summary>
    string FunctionName { get; }
    
    /// <summary>
    /// 설명
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// 입력
    /// </summary>
    object Input { get; }
    
    /// <summary>
    /// 출력
    /// </summary>
    object Output { get; }
    
    /// <summary>
    /// 실행 시간
    /// </summary>
    DateTime ExecutedAt { get; }
    
    /// <summary>
    /// 소요 시간
    /// </summary>
    TimeSpan Duration { get; }
    
    /// <summary>
    /// 성공 여부
    /// </summary>
    bool IsSuccess { get; }
    
    /// <summary>
    /// 오류 메시지
    /// </summary>
    string? ErrorMessage { get; }
}