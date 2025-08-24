namespace Agent.Abstractions.Core.Schema.Validation;
/// <summary>
/// 검증 경고
/// </summary>
public sealed record ValidationWarning
{
    /// <summary>
    /// JSON 경로
    /// </summary>
    public required string Path { get; init; }
    
    /// <summary>
    /// 경고 메시지
    /// </summary>
    public required string Message { get; init; }
    
    /// <summary>
    /// 권장 값 또는 형식
    /// </summary>
    public string? Suggestion { get; init; }
}