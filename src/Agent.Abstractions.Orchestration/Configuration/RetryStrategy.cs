namespace Agent.Abstractions.Orchestration.Configuration;

/// <summary>
/// 재시도 전략
/// </summary>
public enum RetryStrategy
{
    /// <summary>고정 간격</summary>
    Fixed,
    
    /// <summary>선형 증가</summary>
    Linear,
    
    /// <summary>지수 백오프</summary>
    ExponentialBackoff
}