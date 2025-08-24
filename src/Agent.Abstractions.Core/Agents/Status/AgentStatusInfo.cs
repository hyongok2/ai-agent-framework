namespace Agent.Abstractions.Core.Agents;

/// <summary>
/// 에이전트 상태 정보
/// </summary>
public sealed record AgentStatusInfo
{
    /// <summary>상태</summary>
    public required AgentStatus Status { get; init; }
    
    /// <summary>상태 메시지</summary>
    public string? Message { get; init; }
    
    /// <summary>시작 시간</summary>
    public DateTimeOffset? StartedAt { get; init; }
    
    /// <summary>가동 시간</summary>
    public TimeSpan? Uptime { get; init; }
    
    /// <summary>총 요청 수</summary>
    public long TotalRequests { get; init; }
    
    /// <summary>성공 요청 수</summary>
    public long SuccessfulRequests { get; init; }
    
    /// <summary>실패 요청 수</summary>
    public long FailedRequests { get; init; }
    
    /// <summary>평균 응답 시간 (ms)</summary>
    public double AverageResponseTimeMs { get; init; }
    
    /// <summary>활성 대화 수</summary>
    public int ActiveConversations { get; init; }
    
    /// <summary>메모리 사용량 (bytes)</summary>
    public long MemoryUsage { get; init; }
}