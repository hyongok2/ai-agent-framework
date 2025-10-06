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
}
