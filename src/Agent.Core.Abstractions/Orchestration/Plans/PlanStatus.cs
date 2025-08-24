namespace Agent.Core.Abstractions.Orchestration.Plans;

/// <summary>
/// 계획 상태
/// </summary>
public enum PlanStatus
{
    /// <summary>준비됨</summary>
    Ready,
    
    /// <summary>실행 중</summary>
    Running,
    
    /// <summary>일시정지</summary>
    Paused,
    
    /// <summary>완료</summary>
    Completed,
    
    /// <summary>실패</summary>
    Failed,
    
    /// <summary>취소됨</summary>
    Cancelled
}