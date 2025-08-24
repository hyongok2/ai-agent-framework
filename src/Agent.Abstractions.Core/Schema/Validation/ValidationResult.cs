namespace Agent.Abstractions.Core.Schema.Validation;
/// <summary>
/// 검증 결과
/// </summary>
public sealed record ValidationResult
{
    /// <summary>
    /// 검증 성공 여부
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// 검증 오류 목록
    /// </summary>
    public ValidationError[] Errors { get; init; } = Array.Empty<ValidationError>();

    /// <summary>
    /// 경고 목록 (유효하지만 권장사항)
    /// </summary>
    public ValidationWarning[] Warnings { get; init; } = Array.Empty<ValidationWarning>();
}