namespace AIAgentFramework.LLM.Services.Evaluation;

/// <summary>
/// Evaluator 평가 결과
/// </summary>
public class EvaluationResult
{
    /// <summary>
    /// 전체적인 성공 여부
    /// </summary>
    public required bool IsSuccess { get; init; }

    /// <summary>
    /// 품질 점수 (0.0 ~ 1.0)
    /// </summary>
    public required double QualityScore { get; init; }

    /// <summary>
    /// 전반적인 평가 요약
    /// </summary>
    public required string Assessment { get; init; }

    /// <summary>
    /// 강점 목록
    /// </summary>
    public required List<string> Strengths { get; init; } = new();

    /// <summary>
    /// 약점/문제점 목록
    /// </summary>
    public required List<string> Weaknesses { get; init; } = new();

    /// <summary>
    /// 개선 권장사항
    /// </summary>
    public required List<string> Recommendations { get; init; } = new();

    /// <summary>
    /// 평가 기준 충족 여부
    /// </summary>
    public required bool MeetsCriteria { get; init; }

    /// <summary>
    /// 파싱 실패 시 오류 메시지
    /// </summary>
    public string? ErrorMessage { get; init; }
}
