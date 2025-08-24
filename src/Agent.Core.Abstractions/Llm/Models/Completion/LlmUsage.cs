using System.Collections.Generic;

namespace Agent.Core.Abstractions.Llm.Models.Completion;

/// <summary>
/// LLM 사용량 정보
/// </summary>
public sealed record LlmUsage
{
    /// <summary>
    /// 입력 토큰 수
    /// </summary>
    public int PromptTokens { get; init; }
    
    /// <summary>
    /// 출력 토큰 수
    /// </summary>
    public int CompletionTokens { get; init; }
    
    /// <summary>
    /// 총 토큰 수
    /// </summary>
    public int TotalTokens => PromptTokens + CompletionTokens;
    
    /// <summary>
    /// 캐시된 토큰 수
    /// </summary>
    public int? CachedTokens { get; init; }
    
    /// <summary>
    /// 추정 비용 (USD)
    /// </summary>
    public decimal? EstimatedCost { get; init; }
    
    /// <summary>
    /// 추가 메트릭
    /// </summary>
    public IDictionary<string, object> AdditionalMetrics { get; init; } = new Dictionary<string, object>();
}