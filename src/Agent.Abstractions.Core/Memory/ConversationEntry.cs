using Agent.Abstractions.Core.Common.Identifiers;

namespace Agent.Abstractions.Core.Memory;

/// <summary>
/// 대화 항목
/// </summary>
public sealed record ConversationEntry
{
    /// <summary>항목 고유 ID</summary>
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    
    /// <summary>실행 ID (관련된 실행과 연결)</summary>
    public RunId? RunId { get; init; }
    
    /// <summary>역할 (User, Assistant, System)</summary>
    public required ConversationRole Role { get; init; }
    
    /// <summary>메시지 내용</summary>
    public required string Content { get; init; }
    
    /// <summary>메타데이터</summary>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } 
        = new Dictionary<string, object>();
    
    /// <summary>토큰 수 (추정)</summary>
    public int? TokenCount { get; init; }
    
    /// <summary>생성 시간</summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    
    /// <summary>중요도 (0-10, 높을수록 중요)</summary>
    public int Importance { get; init; } = 5;
    
    /// <summary>
    /// 사용자 메시지 생성
    /// </summary>
    /// <param name="content">메시지 내용</param>
    /// <param name="runId">실행 ID</param>
    /// <returns>사용자 대화 항목</returns>
    public static ConversationEntry CreateUserMessage(string content, RunId? runId = null)
        => new()
        {
            Role = ConversationRole.User,
            Content = content,
            RunId = runId
        };
    
    /// <summary>
    /// 어시스턴트 응답 생성
    /// </summary>
    /// <param name="content">응답 내용</param>
    /// <param name="runId">실행 ID</param>
    /// <param name="tokenCount">토큰 수</param>
    /// <returns>어시스턴트 대화 항목</returns>
    public static ConversationEntry CreateAssistantMessage(string content, RunId? runId = null, int? tokenCount = null)
        => new()
        {
            Role = ConversationRole.Assistant,
            Content = content,
            RunId = runId,
            TokenCount = tokenCount
        };
    
    /// <summary>
    /// 시스템 메시지 생성
    /// </summary>
    /// <param name="content">시스템 메시지</param>
    /// <returns>시스템 대화 항목</returns>
    public static ConversationEntry CreateSystemMessage(string content)
        => new()
        {
            Role = ConversationRole.System,
            Content = content,
            Importance = 8 // 시스템 메시지는 높은 중요도
        };
}