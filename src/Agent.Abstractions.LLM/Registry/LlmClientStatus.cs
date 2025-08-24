using Agent.Abstractions.LLM.Enums;

namespace Agent.Abstractions.LLM.Registry;

/// <summary>
/// LLM 클라이언트 상태
/// </summary>
public sealed record LlmClientStatus
{
    /// <summary>
    /// 공급자명
    /// </summary>
    public required string Provider { get; init; }
    
    /// <summary>
    /// 상태
    /// </summary>
    public ClientStatus Status { get; init; }
    
    /// <summary>
    /// 사용 가능 여부
    /// </summary>
    public bool IsAvailable { get; init; }
    
    /// <summary>
    /// 응답 시간 (밀리초)
    /// </summary>
    public long? ResponseTimeMs { get; init; }
    
    /// <summary>
    /// 에러 메시지
    /// </summary>
    public string? ErrorMessage { get; init; }
    
    /// <summary>
    /// 마지막 확인 시간
    /// </summary>
    public DateTimeOffset LastChecked { get; init; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// 통계 정보
    /// </summary>
    public LlmClientStats? Stats { get; init; }
}