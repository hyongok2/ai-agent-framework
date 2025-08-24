using System;

namespace Agent.Abstractions.Core.Agents;

/// <summary>
/// 성능 메트릭
/// </summary>
public sealed record PerformanceMetrics
{
    /// <summary>
    /// 총 실행 시간
    /// </summary>
    public TimeSpan TotalDuration { get; init; }
    
    /// <summary>
    /// LLM 응답 시간
    /// </summary>
    public TimeSpan? LlmDuration { get; init; }
    
    /// <summary>
    /// 도구 실행 시간
    /// </summary>
    public TimeSpan? ToolsDuration { get; init; }
    
    /// <summary>
    /// 오케스트레이션 시간
    /// </summary>
    public TimeSpan? OrchestrationDuration { get; init; }
    
    /// <summary>
    /// 첫 토큰까지 시간 (TTFT)
    /// </summary>
    public TimeSpan? TimeToFirstToken { get; init; }
    
    /// <summary>
    /// 초당 토큰 수
    /// </summary>
    public double? TokensPerSecond { get; init; }
    
    /// <summary>
    /// 큐 대기 시간
    /// </summary>
    public TimeSpan? QueueTime { get; init; }
}