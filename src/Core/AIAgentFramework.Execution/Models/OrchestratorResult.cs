using AIAgentFramework.Core.Abstractions;

namespace AIAgentFramework.Execution.Models;

/// <summary>
/// Orchestrator 실행 결과
/// </summary>
public class OrchestratorResult : IResult
{
    public required bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public required DateTimeOffset CompletedAt { get; init; }

    // Orchestrator 전용 정보
    public string? PlanSummary { get; init; }
    public string? ExecutionSummary { get; init; }
    public int? EvaluationScore { get; init; }
    public string? EvaluationSummary { get; init; }
    public List<string>? Improvements { get; init; }
}
