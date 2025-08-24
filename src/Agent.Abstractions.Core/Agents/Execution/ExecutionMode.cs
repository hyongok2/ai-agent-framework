namespace Agent.Abstractions.Core.Agents;

/// <summary>
/// 실행 모드
/// </summary>
public enum ExecutionMode
{
    /// <summary>자동 선택</summary>
    Auto,
    
    /// <summary>단순 실행</summary>
    Simple,
    
    /// <summary>계획 수립</summary>
    Planner,
    
    /// <summary>반응형</summary>
    Reactive,
    
    /// <summary>고정 워크플로우</summary>
    Fixed
}