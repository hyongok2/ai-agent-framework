namespace AIAgentFramework.LLM.Services.IntentAnalysis;

/// <summary>
/// 의도 분석 입력
/// </summary>
public class IntentAnalysisInput
{
    /// <summary>
    /// 사용자 입력
    /// </summary>
    public string UserInput { get; set; } = string.Empty;

    /// <summary>
    /// 대화 히스토리 (선택적)
    /// </summary>
    public string? ConversationHistory { get; set; }

    /// <summary>
    /// 추가 컨텍스트 (선택적)
    /// </summary>
    public string? Context { get; set; }
}
