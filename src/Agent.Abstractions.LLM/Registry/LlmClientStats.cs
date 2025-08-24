namespace Agent.Abstractions.LLM.Registry;

/// <summary>
/// LLM 클라이언트 통계
/// </summary>
public sealed record LlmClientStats
{
    /// <summary>
    /// 총 요청 수
    /// </summary>
    public long TotalRequests { get; init; }
    
    /// <summary>
    /// 성공 요청 수
    /// </summary>
    public long SuccessfulRequests { get; init; }
    
    /// <summary>
    /// 실패 요청 수
    /// </summary>
    public long FailedRequests { get; init; }
    
    /// <summary>
    /// 평균 응답 시간 (밀리초)
    /// </summary>
    public double AverageResponseTimeMs { get; init; }
    
    /// <summary>
    /// 총 토큰 사용량
    /// </summary>
    public long TotalTokens { get; init; }
    
    /// <summary>
    /// 총 비용 (USD)
    /// </summary>
    public decimal TotalCost { get; init; }
    
    /// <summary>
    /// 가용성 (%)
    /// </summary>
    public double Availability { get; init; }
    
    /// <summary>
    /// 통계 수집 기간
    /// </summary>
    public TimeSpan Period { get; init; }
}