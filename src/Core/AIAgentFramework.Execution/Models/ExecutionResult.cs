using AIAgentFramework.Core.Abstractions;

namespace AIAgentFramework.Execution.Models;

/// <summary>
/// Plan 실행 결과
/// - IResult: 실행 메타정보 (성공 여부, 시간 등)
/// - 단계별 상세 결과 및 요약 정보
/// </summary>
public class ExecutionResult : IResult
{
    // IResult 구현
    public required bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public required DateTimeOffset CompletedAt { get; init; }

    // ExecutionResult 고유 속성
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
    /// 성공한 단계 수
    /// </summary>
    public int SuccessfulSteps => Steps.Count(s => s.IsSuccess);

    /// <summary>
    /// 실패한 단계 수
    /// </summary>
    public int FailedSteps => Steps.Count(s => !s.IsSuccess);
}
