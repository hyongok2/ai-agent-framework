namespace AIAgentFramework.LLM.Services.Planning;

/// <summary>
/// Task Planning 결과
/// </summary>
public class PlanningResult
{
    /// <summary>
    /// 계획 타입 - 사용자 요청의 유형 (도구 실행, 단순 응답 등)
    /// </summary>
    public PlanType Type { get; init; } = PlanType.ToolExecution;

    /// <summary>
    /// 계획 요약
    /// </summary>
    public required string Summary { get; init; }

    /// <summary>
    /// 단순 응답/정보 제공 시 직접 응답 내용
    /// </summary>
    public string? DirectResponse { get; init; }

    /// <summary>
    /// 실행 단계 목록
    /// </summary>
    public required List<TaskStep> Steps { get; init; }

    /// <summary>
    /// 전체 예상 실행 시간 (초)
    /// </summary>
    public int TotalEstimatedSeconds { get; init; }

    /// <summary>
    /// 계획이 실행 가능한지 여부
    /// </summary>
    public bool IsExecutable { get; init; } = true;

    /// <summary>
    /// 실행 불가능한 경우 이유
    /// </summary>
    public string? ExecutionBlocker { get; init; }

    /// <summary>
    /// 계획 생성 시 고려된 제약사항
    /// </summary>
    public List<string> Constraints { get; init; } = new();
}
