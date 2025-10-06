namespace AIAgentFramework.Execution.Models;

/// <summary>
/// 개별 단계 실행 결과
/// </summary>
public class StepExecutionResult
{
    /// <summary>
    /// 단계 번호
    /// </summary>
    public required int StepNumber { get; init; }

    /// <summary>
    /// 단계 설명
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// 사용된 Tool 이름
    /// </summary>
    public required string ToolName { get; init; }

    /// <summary>
    /// 생성된 파라미터
    /// </summary>
    public string? Parameters { get; init; }

    /// <summary>
    /// 실행 성공 여부
    /// </summary>
    public required bool IsSuccess { get; init; }

    /// <summary>
    /// Tool 실행 결과
    /// </summary>
    public string? Output { get; init; }

    /// <summary>
    /// 오류 메시지
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// 실행 시간 (밀리초)
    /// </summary>
    public long ExecutionTimeMs { get; init; }

    /// <summary>
    /// 출력 변수명 (다음 단계에서 참조)
    /// </summary>
    public string? OutputVariable { get; init; }
}
