using System;
using System.Text.Json.Nodes;

namespace Agent.Core.Abstractions.Llm.Models.Functions;

/// <summary>
/// 함수 호출 정보
/// </summary>
public sealed record FunctionCall
{
    /// <summary>
    /// 함수명
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// 매개변수 (JSON)
    /// </summary>
    public required JsonNode Arguments { get; init; }
    
    /// <summary>
    /// 호출 ID
    /// </summary>
    public string CallId { get; init; } = Guid.NewGuid().ToString();
}