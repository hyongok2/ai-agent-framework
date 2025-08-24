using System;
using System.Text.Json.Serialization;

namespace Agent.Abstractions.Core.Agents;

/// <summary>
/// 사용량 정보
/// </summary>
public sealed record UsageInfo
{
    /// <summary>
    /// 입력 토큰
    /// </summary>
    public int InputTokens { get; init; }
    
    /// <summary>
    /// 출력 토큰
    /// </summary>
    public int OutputTokens { get; init; }
    
    /// <summary>
    /// 총 토큰
    /// </summary>
    [JsonIgnore]
    public int TotalTokens => InputTokens + OutputTokens;
    
    /// <summary>
    /// 도구 호출 수
    /// </summary>
    public int ToolCalls { get; init; }
    
    /// <summary>
    /// LLM 호출 수
    /// </summary>
    public int LlmCalls { get; init; }
    
    /// <summary>
    /// 추정 비용 (USD)
    /// </summary>
    public decimal? EstimatedCost { get; init; }
    
    /// <summary>
    /// 메모리 사용량 (bytes)
    /// </summary>
    public long? MemoryUsed { get; init; }
}
