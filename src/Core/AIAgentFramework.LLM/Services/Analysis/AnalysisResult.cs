namespace AIAgentFramework.LLM.Services.Analysis;

/// <summary>
/// Analyzer 결과
/// </summary>
public class AnalysisResult
{
    /// <summary>
    /// 주요 의도 또는 목적
    /// </summary>
    public required string Intent { get; init; }

    /// <summary>
    /// 추출된 핵심 엔티티 목록
    /// </summary>
    public required List<string> Entities { get; init; }

    /// <summary>
    /// 감정 분석 결과 (positive, negative, neutral)
    /// </summary>
    public string? Sentiment { get; init; }

    /// <summary>
    /// 신뢰도 점수 (0.0 ~ 1.0)
    /// </summary>
    public required double Confidence { get; init; }

    /// <summary>
    /// 상세 분석 내용
    /// </summary>
    public string? DetailedAnalysis { get; init; }

    /// <summary>
    /// 모호한 부분 또는 명확화가 필요한 사항
    /// </summary>
    public List<string>? Ambiguities { get; init; }

    /// <summary>
    /// 오류 메시지 (실패 시)
    /// </summary>
    public string? ErrorMessage { get; init; }
}
