namespace AIAgentFramework.Core.Models;

/// <summary>
/// 검증 결과
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// 검증 성공 여부
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 오류 메시지 목록
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// 성공 결과 생성
    /// </summary>
    public static ValidationResult Success() => new() { IsValid = true };

    /// <summary>
    /// 실패 결과 생성
    /// </summary>
    public static ValidationResult Failure(params string[] errors) => new() 
    { 
        IsValid = false, 
        Errors = errors.ToList() 
    };
}