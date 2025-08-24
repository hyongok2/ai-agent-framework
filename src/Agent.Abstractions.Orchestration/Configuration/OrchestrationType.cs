namespace Agent.Abstractions.Orchestration.Configuration;

/// <summary>
/// 오케스트레이션 타입
/// </summary>
public enum OrchestrationType
{
    /// <summary>단순 실행</summary>
    Simple,
    
    /// <summary>고정 워크플로우</summary>
    Fixed,
    
    /// <summary>동적 계획</summary>
    Planner,
    
    /// <summary>반응형 실행</summary>
    Reactive
}