using System;
using Agent.Abstractions.Core.Common.Identifiers;

namespace Agent.Abstractions.Core.Agents;

/// <summary>
/// 실행 단계 결과
/// </summary>
public sealed record ExecutionStepResult
{
    /// <summary>
    /// 단계 ID
    /// </summary>
    public required StepId StepId { get; init; }
    
    /// <summary>
    /// 단계 이름
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// 단계 타입
    /// </summary>
    public required string Type { get; init; }
    
    /// <summary>
    /// 상태
    /// </summary>
    public required string Status { get; init; }
    
    /// <summary>
    /// 시작 시간
    /// </summary>
    public DateTimeOffset StartedAt { get; init; }
    
    /// <summary>
    /// 완료 시간
    /// </summary>
    public DateTimeOffset? CompletedAt { get; init; }
    
    /// <summary>
    /// 소요 시간
    /// </summary>
    public TimeSpan? Duration => CompletedAt - StartedAt;
    
    /// <summary>
    /// 출력 데이터
    /// </summary>
    public JsonDocument? Output { get; init; }
    
    /// <summary>
    /// 에러 정보
    /// </summary>
    public string? Error { get; init; }
}