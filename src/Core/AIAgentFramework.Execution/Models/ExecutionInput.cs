using AIAgentFramework.LLM.Services.Planning;

namespace AIAgentFramework.Execution.Models;

/// <summary>
/// Executor 입력 정보
/// </summary>
public class ExecutionInput
{
    /// <summary>
    /// 실행할 계획
    /// </summary>
    public required PlanningResult Plan { get; init; }

    /// <summary>
    /// 원본 사용자 요청
    /// </summary>
    public required string UserRequest { get; init; }

    /// <summary>
    /// 추가 컨텍스트 (선택적)
    /// </summary>
    public string? AdditionalContext { get; init; }
}
