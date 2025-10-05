namespace AIAgentFramework.LLM.Models;

/// <summary>
/// 프롬프트 변수 검증 결과
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; init; }
    public IReadOnlyList<string> MissingVariables { get; init; } = Array.Empty<string>();
    public string ErrorMessage => IsValid
        ? string.Empty
        : $"필수 변수 누락: {string.Join(", ", MissingVariables)}";
}
