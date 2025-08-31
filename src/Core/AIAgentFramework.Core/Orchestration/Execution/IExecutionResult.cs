namespace AIAgentFramework.Core.Orchestration.Execution;


/// <summary>
/// 실행 결과 인터페이스
/// </summary>
public interface IExecutionResult
{
    /// <summary>
    /// 명령 ID
    /// </summary>
    string CommandId { get; }
    
    /// <summary>
    /// 성공 여부
    /// </summary>
    bool IsSuccess { get; }
    
    /// <summary>
    /// 결과 데이터
    /// </summary>
    object? Data { get; }
    
    /// <summary>
    /// 실행 시간
    /// </summary>
    DateTime ExecutedAt { get; }
    
    /// <summary>
    /// 실행 소요 시간
    /// </summary>
    TimeSpan Duration { get; }
    
    /// <summary>
    /// 오류 메시지 (실패 시)
    /// </summary>
    string? ErrorMessage { get; }
    
    /// <summary>
    /// 메타데이터
    /// </summary>
    Dictionary<string, object> Metadata { get; }
}