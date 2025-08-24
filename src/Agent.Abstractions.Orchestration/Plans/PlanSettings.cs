using Agent.Abstractions.Orchestration.Execution;

namespace Agent.Abstractions.Orchestration.Plans;

/// <summary>
/// 계획 설정
/// </summary>
public sealed record PlanSettings
{
    /// <summary>
    /// 최대 실행 시간
    /// </summary>
    public TimeSpan? MaxExecutionTime { get; init; }
    
    /// <summary>
    /// 병렬 실행 최대 개수
    /// </summary>
    public int MaxParallelSteps { get; init; } = 5;
    
    /// <summary>
    /// 실패 시 중단 여부
    /// </summary>
    public bool StopOnFirstFailure { get; init; } = true;
    
    /// <summary>
    /// 재시도 정책 (전역)
    /// </summary>
    public RetryPolicy? DefaultRetryPolicy { get; init; }
    
    /// <summary>
    /// 로깅 레벨
    /// </summary>
    public Configuration.LogLevel LogLevel { get; init; } = Configuration.LogLevel.Information;
}
