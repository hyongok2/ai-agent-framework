using Agent.Abstractions.LLM.Enums;

namespace Agent.Abstractions.LLM.Registry;

/// <summary>
/// LLM 모델 정보
/// </summary>
public sealed record LlmModel
{
    /// <summary>
    /// 모델 ID
    /// </summary>
    public required string Id { get; init; }
    
    /// <summary>
    /// 모델 이름
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// 설명
    /// </summary>
    public string? Description { get; init; }
    
    /// <summary>
    /// 공급자
    /// </summary>
    public required string Provider { get; init; }
    
    /// <summary>
    /// 모델 타입
    /// </summary>
    public ModelType Type { get; init; } = ModelType.TextGeneration;
    
    /// <summary>
    /// 최대 토큰 수
    /// </summary>
    public int? MaxTokens { get; init; }
    
    /// <summary>
    /// 최대 출력 토큰 수
    /// </summary>
    public int? MaxOutputTokens { get; init; }
    
    /// <summary>
    /// 지원 기능
    /// </summary>
    public LlmCapabilities Capabilities { get; init; } = new();
    
    /// <summary>
    /// 사용 가능 여부
    /// </summary>
    public bool IsAvailable { get; init; } = true;
    
    /// <summary>
    /// 지원 언어
    /// </summary>
    public IReadOnlyList<string> SupportedLanguages { get; init; } = Array.Empty<string>();
    
    /// <summary>
    /// 메타데이터
    /// </summary>
    public IDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
}