namespace AIAgentFramework.LLM.Services.Planning;

/// <summary>
/// Task Planning 입력 데이터
/// </summary>
public class PlanningInput
{
    /// <summary>
    /// 사용자의 원본 요청
    /// </summary>
    public required string UserRequest { get; init; }

    /// <summary>
    /// 현재 실행 컨텍스트 (선택적)
    /// </summary>
    public string? Context { get; init; }

    /// <summary>
    /// 대화 히스토리 (선택적)
    /// </summary>
    public string? ConversationHistory { get; init; }

    /// <summary>
    /// 이전 실행 결과 (선택적)
    /// </summary>
    public string? PreviousResults { get; init; }

    /// <summary>
    /// 반복 시도 번호 (1부터 시작, 최대 5)
    /// </summary>
    public int IterationNumber { get; init; } = 1;

    /// <summary>
    /// 이전 시도의 실행 결과 요약 (재시도 시)
    /// </summary>
    public string? PreviousAttemptSummary { get; init; }

    /// <summary>
    /// 이전 시도의 평가 피드백 (재시도 시)
    /// </summary>
    public string? EvaluationFeedback { get; init; }

    /// <summary>
    /// 이전 시도에서 실패한 이유 (재시도 시)
    /// </summary>
    public string? FailureReason { get; init; }
}
