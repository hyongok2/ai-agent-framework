using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace Agent.Core.Abstractions.Llm.Models.Functions;

/// <summary>
/// LLM 함수 정의
/// </summary>
public sealed record LlmFunction
{
    /// <summary>
    /// 함수명
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// 함수 설명
    /// </summary>
    public required string Description { get; init; }
    
    /// <summary>
    /// 매개변수 스키마
    /// </summary>
    public required JsonNode Parameters { get; init; }
    
    /// <summary>
    /// 필수 매개변수 목록
    /// </summary>
    public IReadOnlyList<string> Required { get; init; } = Array.Empty<string>();
    
    /// <summary>
    /// 예제
    /// </summary>
    public IReadOnlyList<FunctionExample> Examples { get; init; } = Array.Empty<FunctionExample>();
}