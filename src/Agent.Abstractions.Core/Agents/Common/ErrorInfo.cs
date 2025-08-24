using System;

namespace Agent.Abstractions.Core.Agents;

/// <summary>
/// 에러 정보
/// </summary>
public sealed record ErrorInfo
{
    /// <summary>
    /// 에러 코드
    /// </summary>
    public required string Code { get; init; }
    
    /// <summary>
    /// 에러 메시지
    /// </summary>
    public required string Message { get; init; }
    
    /// <summary>
    /// 에러 타입
    /// </summary>
    public ErrorType Type { get; init; } = ErrorType.Unknown;
    
    /// <summary>
    /// 상세 정보
    /// </summary>
    public string? Details { get; init; }
    
    /// <summary>
    /// 스택 트레이스 (디버그 모드)
    /// </summary>
    public string? StackTrace { get; init; }
    
    /// <summary>
    /// 재시도 가능 여부
    /// </summary>
    public bool IsRetryable { get; init; }
    
    /// <summary>
    /// 재시도 후 시간 (초)
    /// </summary>
    public int? RetryAfterSeconds { get; init; }
    
    /// <summary>
    /// 내부 에러
    /// </summary>
    public ErrorInfo? InnerError { get; init; }
    
    /// <summary>
    /// 에러 발생 시간
    /// </summary>
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}