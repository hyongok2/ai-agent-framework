namespace AIAgentFramework.LLM.Services.Conversation;

/// <summary>
/// 대화 입력
/// </summary>
public class ConversationInput
{
    /// <summary>
    /// 사용자 메시지
    /// </summary>
    public required string UserMessage { get; init; }

    /// <summary>
    /// 대화 히스토리 (선택)
    /// </summary>
    public string? ConversationHistory { get; init; }

    /// <summary>
    /// 시스템 컨텍스트 (선택)
    /// </summary>
    public string? SystemContext { get; init; }
}
