using Agent.Abstractions.LLM.Enums;
using Agent.Abstractions.LLM.Models.Functions;

namespace Agent.Abstractions.LLM.Models.Completion;

/// <summary>
/// LLM 요청
/// </summary>
public sealed record LlmRequest
{
    /// <summary>
    /// 모델명
    /// </summary>
    public required string Model { get; init; }
    
    /// <summary>
    /// 메시지 목록
    /// </summary>
    public required IReadOnlyList<LlmMessage> Messages { get; init; }
    
    /// <summary>
    /// 시스템 프롬프트
    /// </summary>
    public string? SystemPrompt { get; init; }
    
    /// <summary>
    /// 최대 토큰 수 (출력)
    /// </summary>
    public int? MaxTokens { get; init; }
    
    /// <summary>
    /// 온도 (0.0 ~ 2.0)
    /// </summary>
    public double? Temperature { get; init; }
    
    /// <summary>
    /// Top-P 샘플링
    /// </summary>
    public double? TopP { get; init; }
    
    /// <summary>
    /// 빈도 페널티
    /// </summary>
    public double? FrequencyPenalty { get; init; }
    
    /// <summary>
    /// 존재 페널티
    /// </summary>
    public double? PresencePenalty { get; init; }
    
    /// <summary>
    /// 정지 단어
    /// </summary>
    public IReadOnlyList<string> StopWords { get; init; } = Array.Empty<string>();
    
    /// <summary>
    /// 시드 (재현 가능한 생성)
    /// </summary>
    public int? Seed { get; init; }
    
    /// <summary>
    /// JSON 모드 활성화
    /// </summary>
    public bool JsonMode { get; init; }
    
    /// <summary>
    /// 함수 목록
    /// </summary>
    public IReadOnlyList<LlmFunction> Functions { get; init; } = Array.Empty<LlmFunction>();
    
    /// <summary>
    /// 함수 호출 모드
    /// </summary>
    public FunctionCallMode FunctionCallMode { get; init; } = FunctionCallMode.Auto;
    
    /// <summary>
    /// 강제할 함수명
    /// </summary>
    public string? ForceFunction { get; init; }
    
    /// <summary>
    /// 메타데이터
    /// </summary>
    public IDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
    
    /// <summary>
    /// 요청 ID
    /// </summary>
    public string RequestId { get; init; } = Guid.NewGuid().ToString();
}