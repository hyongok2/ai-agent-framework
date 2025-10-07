namespace AIAgentFramework.LLM.Services.Universal;

/// <summary>
/// UniversalLLM 입력
/// 모든 LLM 작업을 처리할 수 있는 범용 입력 구조
/// </summary>
public class UniversalLLMInput
{
    /// <summary>
    /// 작업 유형 (Summarize, Analyze, Convert, Generate 등)
    /// </summary>
    public string TaskType { get; set; } = string.Empty;

    /// <summary>
    /// 처리할 내용
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 페르소나 (역할 설정)
    /// </summary>
    public string? Persona { get; set; }

    /// <summary>
    /// 응답 가이드 (동적 프롬프트 지시사항)
    /// </summary>
    public ResponseGuide ResponseGuide { get; set; } = new();

    /// <summary>
    /// 추가 컨텍스트 (선택적)
    /// </summary>
    public string? AdditionalContext { get; set; }
}
