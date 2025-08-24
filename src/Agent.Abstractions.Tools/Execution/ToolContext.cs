using Agent.Abstractions.Core.Common.Identifiers;

namespace Agent.Abstractions.Tools.Execution;

/// <summary>
/// 도구 실행 컨텍스트
/// </summary>
public sealed record ToolContext
{
    /// <summary>
    /// 실행 ID
    /// </summary>
    public required RunId RunId { get; init; }
    
    /// <summary>
    /// Step ID
    /// </summary>
    public required StepId StepId { get; init; }
    
    /// <summary>
    /// 사용자 ID
    /// </summary>
    public string? UserId { get; init; }
    
    /// <summary>
    /// 세션 ID
    /// </summary>
    public string? SessionId { get; init; }
    
    /// <summary>
    /// 실행 환경 정보
    /// </summary>
    public ExecutionEnvironment Environment { get; init; } = new();
    
    /// <summary>
    /// 메타데이터
    /// </summary>
    public IDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
    
    /// <summary>
    /// 추적 정보
    /// </summary>
    public string? TraceId { get; init; }
    
    /// <summary>
    /// 부모 스팬 ID
    /// </summary>
    public string? ParentSpanId { get; init; }
}