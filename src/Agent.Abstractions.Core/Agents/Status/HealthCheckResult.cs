namespace Agent.Abstractions.Core.Agents;

/// <summary>
/// 헬스 체크 결과
/// </summary>
public sealed record HealthCheckResult
{
    /// <summary>건강 상태</summary>
    public HealthStatus Status { get; init; }
    
    /// <summary>체크 항목</summary>
    public Dictionary<string, ComponentHealth> Components { get; init; } = new();
    
    /// <summary>총 체크 시간</summary>
    public TimeSpan CheckDuration { get; init; }
}