namespace Agent.Core.Abstractions.Orchestration.Execution;

/// <summary>
/// Step 상태
/// </summary>
public enum StepStatus
{
    /// <summary>대기 중</summary>
    Pending,
    
    /// <summary>실행 중</summary>
    Running,
    
    /// <summary>완료</summary>
    Completed,
    
    /// <summary>실패</summary>
    Failed,
    
    /// <summary>건너뜀</summary>
    Skipped,
    
    /// <summary>취소됨</summary>
    Cancelled,
    
    /// <summary>재시도 중</summary>
    Retrying
}