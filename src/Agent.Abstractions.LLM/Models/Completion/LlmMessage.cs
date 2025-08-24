using Agent.Abstractions.LLM.Enums;
using Agent.Abstractions.LLM.Models.Functions;

namespace Agent.Abstractions.LLM.Models.Completion;

/// <summary>
/// LLM 메시지
/// </summary>
public sealed record LlmMessage
{
    /// <summary>
    /// 메시지 역할
    /// </summary>
    public required MessageRole Role { get; init; }
    
    /// <summary>
    /// 메시지 내용
    /// </summary>
    public string? Content { get; init; }
    
    /// <summary>
    /// 이미지 URL (multimodal인 경우)
    /// </summary>
    public string? ImageUrl { get; init; }
    
    /// <summary>
    /// 이미지 데이터 (base64)
    /// </summary>
    public string? ImageData { get; init; }
    
    /// <summary>
    /// 함수 호출 정보
    /// </summary>
    public FunctionCall? FunctionCall { get; init; }
    
    /// <summary>
    /// 함수 실행 결과
    /// </summary>
    public string? FunctionResult { get; init; }
    
    /// <summary>
    /// 메시지 이름 (함수 결과인 경우)
    /// </summary>
    public string? Name { get; init; }
    
    /// <summary>
    /// 타임스탬프
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// 메타데이터
    /// </summary>
    public IDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
}