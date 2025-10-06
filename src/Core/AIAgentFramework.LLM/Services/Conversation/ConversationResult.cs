namespace AIAgentFramework.LLM.Services.Conversation;

/// <summary>
/// 대화 응답 결과
/// </summary>
public class ConversationResult
{
    /// <summary>
    /// 응답 메시지
    /// </summary>
    public required string Response { get; init; }

    /// <summary>
    /// 응답 톤 (friendly, professional, casual 등)
    /// </summary>
    public string? Tone { get; init; }

    /// <summary>
    /// 추가 메타데이터
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }
}
