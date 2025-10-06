namespace AIAgentFramework.Execution.Models;

/// <summary>
/// Executor 실행 결과
/// </summary>
public class ExecutionResult
{
    /// <summary>
    /// 전체 실행 성공 여부
    /// </summary>
    public required bool IsSuccess { get; init; }

    /// <summary>
    /// 각 단계별 실행 결과
    /// </summary>
    public required List<StepExecutionResult> Steps { get; init; } = new();

    /// <summary>
    /// 전체 실행 요약
    /// </summary>
    public string? Summary { get; init; }

    /// <summary>
    /// 전체 실행 시간 (밀리초)
    /// </summary>
    public long TotalExecutionTimeMs { get; init; }

    /// <summary>
    /// 실행 실패 시 오류 메시지
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// 성공한 단계 수
    /// </summary>
    public int SuccessfulSteps => Steps.Count(s => s.IsSuccess);

    /// <summary>
    /// 실패한 단계 수
    /// </summary>
    public int FailedSteps => Steps.Count(s => !s.IsSuccess);
}
