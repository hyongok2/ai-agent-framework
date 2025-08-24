using System;
using System.Collections.Generic;

namespace Agent.Core.Abstractions.Llm.Registry;

/// <summary>
/// LLM 공급자 정보
/// </summary>
public sealed record LlmProvider
{
    /// <summary>
    /// 공급자명
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// 표시명
    /// </summary>
    public required string DisplayName { get; init; }
    
    /// <summary>
    /// 설명
    /// </summary>
    public string? Description { get; init; }
    
    /// <summary>
    /// 웹사이트 URL
    /// </summary>
    public string? WebsiteUrl { get; init; }
    
    /// <summary>
    /// 지원하는 모델 목록
    /// </summary>
    public IReadOnlyList<LlmModel> Models { get; init; } = Array.Empty<LlmModel>();
    
    /// <summary>
    /// 활성화 여부
    /// </summary>
    public bool IsEnabled { get; init; } = true;
    
    /// <summary>
    /// 설정 정보
    /// </summary>
    public IReadOnlyDictionary<string, object>? Configuration { get; init; }
    
    /// <summary>
    /// 생성 시간
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}