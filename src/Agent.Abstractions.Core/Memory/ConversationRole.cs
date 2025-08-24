namespace Agent.Abstractions.Core.Memory;

/// <summary>
/// 대화에서의 역할
/// </summary>
public enum ConversationRole
{
    /// <summary>사용자</summary>
    User = 0,
    
    /// <summary>어시스턴트/에이전트</summary>
    Assistant = 1,
    
    /// <summary>시스템</summary>
    System = 2,
    
    /// <summary>도구 호출 결과</summary>
    Tool = 3
}