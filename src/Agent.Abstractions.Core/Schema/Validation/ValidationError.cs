namespace Agent.Abstractions.Core.Schema.Validation;
/// <summary>
/// 검증 오류
/// </summary>
public sealed record ValidationError
{
    /// <summary>
    /// JSON 경로
    /// </summary>
    public required string Path { get; init; }
    
    /// <summary>
    /// 오류 메시지
    /// </summary>
    public required string Message { get; init; }
    
    /// <summary>
    /// 오류 코드
    /// </summary>
    public string? ErrorCode { get; init; }
    
    /// <summary>
    /// 스키마 키워드 (예: "required", "type")
    /// </summary>
    public string? SchemaKeyword { get; init; }
}
