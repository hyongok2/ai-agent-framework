using System;
using Agent.Abstractions.Core.Common.Identifiers;

namespace Agent.Abstractions.Core.Agents;

/// <summary>
/// 도구 호출 결과
/// </summary>
public sealed record ToolCallResult
{
    /// <summary>
    /// 도구 ID
    /// </summary>
    public required ToolId ToolId { get; init; }
    
    /// <summary>
    /// 도구 이름
    /// </summary>
    public required string ToolName { get; init; }
    
    /// <summary>
    /// 입력 인자
    /// </summary>
    public JsonDocument? Input { get; init; }
    
    /// <summary>
    /// 출력 결과
    /// </summary>
    public JsonDocument? Output { get; init; }
    
    /// <summary>
    /// 성공 여부
    /// </summary>
    public bool Success { get; init; }
    
    /// <summary>
    /// 에러 메시지
    /// </summary>
    public string? Error { get; init; }
    
    /// <summary>
    /// 실행 시간
    /// </summary>
    public TimeSpan Duration { get; init; }
    
    /// <summary>
    /// 실행 시각
    /// </summary>
    public DateTimeOffset ExecutedAt { get; init; } = DateTimeOffset.UtcNow;
}
