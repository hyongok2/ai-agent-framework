namespace Agent.Core.Abstractions.Llm.Registry;

/// <summary>
/// LLM 성능 요구사항
/// </summary>
public sealed record LlmPerformanceRequirements
{
    /// <summary>
    /// 최대 응답 시간 (밀리초)
    /// </summary>
    public long? MaxResponseTimeMs { get; init; }
    
    /// <summary>
    /// 최소 처리량 (토큰/초)
    /// </summary>
    public double? MinThroughput { get; init; }
    
    /// <summary>
    /// 최소 가용성 (%)
    /// </summary>
    public double? MinAvailability { get; init; }
    
    /// <summary>
    /// 최대 에러율 (%)
    /// </summary>
    public double? MaxErrorRate { get; init; }
}