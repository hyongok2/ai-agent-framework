namespace AIAgentFramework.LLM.Services.Summarization;

/// <summary>
/// Summarizer 입력 정보
/// </summary>
public class SummarizationInput
{
    /// <summary>
    /// 요약할 컨텐츠
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// 요약 스타일 (brief, standard, detailed, executive, technical)
    /// </summary>
    public string SummaryStyle { get; init; } = "standard";

    /// <summary>
    /// 추가 요구사항 (선택적)
    /// </summary>
    public string? Requirements { get; init; }
}

/// <summary>
/// 요약 스타일
/// </summary>
public static class SummaryStyle
{
    /// <summary>
    /// 간단 요약 (1-2문장)
    /// </summary>
    public const string Brief = "brief";

    /// <summary>
    /// 표준 요약 (3-5문장)
    /// </summary>
    public const string Standard = "standard";

    /// <summary>
    /// 상세 요약 (다중 문단)
    /// </summary>
    public const string Detailed = "detailed";

    /// <summary>
    /// 임원 보고용
    /// </summary>
    public const string Executive = "executive";

    /// <summary>
    /// 기술 문서용
    /// </summary>
    public const string Technical = "technical";
}
