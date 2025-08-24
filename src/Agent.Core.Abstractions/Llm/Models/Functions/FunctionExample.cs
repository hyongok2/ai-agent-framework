using System.Text.Json.Nodes;

namespace Agent.Core.Abstractions.Llm.Models.Functions;

/// <summary>
/// 함수 예제
/// </summary>
public sealed record FunctionExample
{
    /// <summary>
    /// 예제 이름
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// 설명
    /// </summary>
    public string? Description { get; init; }
    
    /// <summary>
    /// 입력 예제
    /// </summary>
    public required JsonNode Input { get; init; }
    
    /// <summary>
    /// 출력 예제
    /// </summary>
    public required JsonNode Output { get; init; }
}