namespace Agent.Core.Abstractions.Llm.Enums;

/// <summary>
/// 메시지 역할
/// </summary>
public enum MessageRole
{
    /// <summary>시스템</summary>
    System,
    
    /// <summary>사용자</summary>
    User,
    
    /// <summary>어시스턴트</summary>
    Assistant,
    
    /// <summary>함수</summary>
    Function,
    
    /// <summary>도구</summary>
    Tool
}