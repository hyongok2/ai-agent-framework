using System;
using System.Collections.Generic;
using System.Text.Json;
using Agent.Core.Abstractions.Common.Identifiers;
using Agent.Core.Abstractions.Orchestration.Configuration;

namespace Agent.Core.Abstractions.Orchestration.Execution;

/// <summary>
/// 실행 가능한 최소 단위
/// </summary>
public sealed record ExecutionStep
{
    public required StepId Id { get; init; }
    public required StepKind Kind { get; init; }
    public required JsonDocument Input { get; init; }
    public JsonDocument? Output { get; init; }
    public string? Error { get; init; }
    public StepStatus Status { get; init; } = StepStatus.Pending;
    
    /// <summary>
    /// 단계 이름 (사용자 친화적)
    /// </summary>
    public string? Name { get; init; }
    
    /// <summary>
    /// 단계 설명
    /// </summary>
    public string? Description { get; init; }
    
    /// <summary>
    /// 재시도 정책
    /// </summary>
    public RetryPolicy? RetryPolicy { get; init; }
    
    /// <summary>
    /// 타임아웃 설정
    /// </summary>
    public TimeSpan? Timeout { get; init; }
    
    /// <summary>
    /// 메타데이터
    /// </summary>
    public IDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
    
    /// <summary>
    /// 의존성 Step ID 목록
    /// </summary>
    public IReadOnlyList<StepId> Dependencies { get; init; } = Array.Empty<StepId>();
    
    /// <summary>
    /// 실행 시작 시간
    /// </summary>
    public DateTimeOffset? StartedAt { get; init; }
    
    /// <summary>
    /// 실행 완료 시간
    /// </summary>
    public DateTimeOffset? CompletedAt { get; init; }
}

