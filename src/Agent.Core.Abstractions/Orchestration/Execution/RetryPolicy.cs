
using Agent.Core.Abstractions.Orchestration.Configuration;

namespace Agent.Core.Abstractions.Orchestration.Execution;
/// <summary>
/// 재시도 정책
/// </summary>
public sealed record RetryPolicy
{
    /// <summary>
    /// 최대 재시도 횟수
    /// </summary>
    public int MaxAttempts { get; init; } = 3;
    
    /// <summary>
    /// 재시도 간격 전략
    /// </summary>
    public RetryStrategy Strategy { get; init; } = RetryStrategy.ExponentialBackoff;
    
    /// <summary>
    /// 초기 대기 시간
    /// </summary>
    public TimeSpan InitialDelay { get; init; } = TimeSpan.FromSeconds(1);
    
    /// <summary>
    /// 최대 대기 시간
    /// </summary>
    public TimeSpan MaxDelay { get; init; } = TimeSpan.FromMinutes(1);
    
    /// <summary>
    /// 재시도 가능한 에러 코드
    /// </summary>
    public IReadOnlyList<string> RetryableErrors { get; init; } = Array.Empty<string>();
}

