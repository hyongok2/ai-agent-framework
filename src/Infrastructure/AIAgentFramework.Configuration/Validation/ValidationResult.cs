namespace AIAgentFramework.Configuration.Validation;

/// <summary>
/// 설정 검증 결과
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// 검증 성공 여부
    /// </summary>
    public bool IsValid { get; set; } = true;
    
    /// <summary>
    /// 오류 목록
    /// </summary>
    public List<string> Errors { get; set; } = new();
    
    /// <summary>
    /// 경고 목록
    /// </summary>
    public List<string> Warnings { get; set; } = new();
    
    /// <summary>
    /// 정보 메시지 목록
    /// </summary>
    public List<string> Information { get; set; } = new();
    
    /// <summary>
    /// 검증 결과 요약
    /// </summary>
    public string Summary => $"Valid: {IsValid}, Errors: {Errors.Count}, Warnings: {Warnings.Count}";
    
    /// <summary>
    /// 모든 메시지를 문자열로 반환
    /// </summary>
    /// <returns>포맷된 메시지 문자열</returns>
    public string GetFormattedMessages()
    {
        var messages = new List<string>();
        
        if (Errors.Any())
        {
            messages.Add("ERRORS:");
            messages.AddRange(Errors.Select(e => $"  - {e}"));
        }
        
        if (Warnings.Any())
        {
            messages.Add("WARNINGS:");
            messages.AddRange(Warnings.Select(w => $"  - {w}"));
        }
        
        if (Information.Any())
        {
            messages.Add("INFORMATION:");
            messages.AddRange(Information.Select(i => $"  - {i}"));
        }
        
        return string.Join(Environment.NewLine, messages);
    }
}