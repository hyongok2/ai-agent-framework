namespace Agent.Abstractions.Core.Agents;

/// <summary>
/// 건강 상태
/// </summary>
public enum HealthStatus
{
    /// <summary>건강</summary>
    Healthy,
    
    /// <summary>부분 장애</summary>
    Degraded,
    
    /// <summary>비정상</summary>
    Unhealthy
}