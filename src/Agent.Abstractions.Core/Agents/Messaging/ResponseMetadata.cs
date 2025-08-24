namespace Agent.Abstractions.Core.Agents;

/// <summary>
/// 응답 메타데이터
/// </summary>
public sealed record ResponseMetadata
{
    /// <summary>
    /// 사용된 모델
    /// </summary>
    public string? Model { get; init; }
    
    /// <summary>
    /// 모델 버전
    /// </summary>
    public string? ModelVersion { get; init; }
    
    /// <summary>
    /// 사용된 프롬프트 템플릿
    /// </summary>
    public string? PromptTemplate { get; init; }
    
    /// <summary>
    /// 실행 타입
    /// </summary>
    public string? ExecutionType { get; init; }
    
    /// <summary>
    /// 신뢰도 점수 (0-1)
    /// </summary>
    public double? ConfidenceScore { get; init; }
    
    /// <summary>
    /// 캐시 히트 여부
    /// </summary>
    public bool CacheHit { get; init; }
    
    /// <summary>
    /// 추가 메타데이터
    /// </summary>
    public Dictionary<string, object> Additional { get; init; } = new();
}