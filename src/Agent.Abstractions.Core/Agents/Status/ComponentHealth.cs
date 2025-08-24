namespace Agent.Abstractions.Core.Agents;

/// <summary>
/// 컴포넌트 건강 상태
/// </summary>
public sealed record ComponentHealth
{
    /// <summary>컴포넌트 이름</summary>
    public required string Name { get; init; }
    
    /// <summary>상태</summary>
    public required HealthStatus Status { get; init; }
    
    /// <summary>메시지</summary>
    public string? Message { get; init; }
}