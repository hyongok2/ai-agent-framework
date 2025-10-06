namespace AIAgentFramework.LLM.Services.Evaluation;

/// <summary>
/// Evaluator 입력 정보
/// </summary>
public class EvaluationInput
{
    /// <summary>
    /// 평가할 작업 설명
    /// </summary>
    public required string TaskDescription { get; init; }

    /// <summary>
    /// 평가할 실행 결과 요약
    /// </summary>
    public required string ExecutionResult { get; init; }

    /// <summary>
    /// 각 단계별 상세 실행 결과
    /// </summary>
    public string? DetailedStepResults { get; init; }

    /// <summary>
    /// 기대 결과 (선택적)
    /// </summary>
    public string? ExpectedOutcome { get; init; }

    /// <summary>
    /// 평가 기준 (선택적, 기본값 사용 시 null)
    /// </summary>
    public string? EvaluationCriteria { get; init; }
}
