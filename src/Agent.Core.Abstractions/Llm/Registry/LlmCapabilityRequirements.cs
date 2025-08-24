namespace Agent.Core.Abstractions.Llm.Registry;

/// <summary>
/// LLM 기능 요구사항
/// </summary>
public sealed record LlmCapabilityRequirements
{
    /// <summary>
    /// 텍스트 완성 필요
    /// </summary>
    public bool? RequiresCompletion { get; init; }
    
    /// <summary>
    /// 스트리밍 필요
    /// </summary>
    public bool? RequiresStreaming { get; init; }
    
    /// <summary>
    /// 임베딩 필요
    /// </summary>
    public bool? RequiresEmbeddings { get; init; }
    
    /// <summary>
    /// 이미지 입력 필요
    /// </summary>
    public bool? RequiresImageInput { get; init; }
    
    /// <summary>
    /// 이미지 생성 필요
    /// </summary>
    public bool? RequiresImageGeneration { get; init; }
    
    /// <summary>
    /// 함수 호출 필요
    /// </summary>
    public bool? RequiresFunctionCalling { get; init; }
    
    /// <summary>
    /// JSON 모드 필요
    /// </summary>
    public bool? RequiresJsonMode { get; init; }
    
    /// <summary>
    /// 최소 토큰 수
    /// </summary>
    public int? MinTokens { get; init; }
    
    /// <summary>
    /// 최소 출력 토큰 수
    /// </summary>
    public int? MinOutputTokens { get; init; }
}