namespace Agent.Abstractions.Core.Agents;

/// <summary>
/// 에이전트 상태
/// </summary>
public enum AgentStatus
{
    /// <summary>생성됨</summary>
    Created,
    
    /// <summary>초기화 중</summary>
    Initializing,
    
    /// <summary>준비됨</summary>
    Ready,
    
    /// <summary>실행 중</summary>
    Running,
    
    /// <summary>일시정지</summary>
    Paused,
    
    /// <summary>중지 중</summary>
    Stopping,
    
    /// <summary>중지됨</summary>
    Stopped,
    
    /// <summary>에러</summary>
    Error,
    
    /// <summary>폐기됨</summary>
    Disposed
}
