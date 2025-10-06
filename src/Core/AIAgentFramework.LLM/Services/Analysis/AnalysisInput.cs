namespace AIAgentFramework.LLM.Services.Analysis;

/// <summary>
/// Analyzer 입력 정보
/// </summary>
public class AnalysisInput
{
    /// <summary>
    /// 분석할 입력 텍스트
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// 분석 목적 (선택적)
    /// </summary>
    public string? Purpose { get; init; }

    /// <summary>
    /// 분석 초점 영역 (선택적)
    /// 예: "intent", "entities", "sentiment", "structure"
    /// </summary>
    public string? FocusArea { get; init; }
}
