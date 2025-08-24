namespace Agent.Abstractions.Core.Agents;

/// <summary>
/// 검증 결과
/// </summary>
public sealed record ValidationResult
{
    /// <summary>
    /// 유효 여부
    /// </summary>
    public bool IsValid { get; init; }
    
    /// <summary>
    /// 에러 목록
    /// </summary>
    public List<string> Errors { get; init; } = new();
}