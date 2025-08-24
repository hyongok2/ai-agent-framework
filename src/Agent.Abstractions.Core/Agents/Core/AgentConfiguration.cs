namespace Agent.Abstractions.Core.Agents;

/// <summary>
/// 에이전트 설정
/// </summary>
public sealed record AgentConfiguration
{
    /// <summary>LLM 모델</summary>
    public string Model { get; init; } = "gpt-4";
    
    /// <summary>온도</summary>
    public double Temperature { get; init; } = 0.7;
    
    /// <summary>최대 토큰</summary>
    public int MaxTokens { get; init; } = 2000;
    
    /// <summary>타임아웃 (초)</summary>
    public int TimeoutSeconds { get; init; } = 30;
    
    /// <summary>재시도 횟수</summary>
    public int MaxRetries { get; init; } = 3;
    
    /// <summary>메모리 사용</summary>
    public bool UseMemory { get; init; } = true;
    
    /// <summary>캐싱 사용</summary>
    public bool UseCaching { get; init; } = true;
    
    /// <summary>디버그 모드</summary>
    public bool DebugMode { get; init; }
    
    /// <summary>시스템 프롬프트</summary>
    public string? SystemPrompt { get; init; }
    
    /// <summary>추가 설정</summary>
    public Dictionary<string, object> AdditionalSettings { get; init; } = new();
}