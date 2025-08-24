namespace Agent.Abstractions.LLM.Models.Streaming;

/// <summary>
/// 함수 호출 델타
/// </summary>
public sealed record FunctionCallDelta
{
    /// <summary>
    /// 함수명 델타
    /// </summary>
    public string? NameDelta { get; init; }
    
    /// <summary>
    /// 매개변수 델타 (JSON 문자열 조각)
    /// </summary>
    public string? ArgumentsDelta { get; init; }
    
    /// <summary>
    /// 호출 ID (첫 청크에서만)
    /// </summary>
    public string? CallId { get; init; }
}