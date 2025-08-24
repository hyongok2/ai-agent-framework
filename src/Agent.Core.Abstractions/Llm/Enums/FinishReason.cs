namespace Agent.Core.Abstractions.Llm.Enums;

/// <summary>
/// 완료 이유
/// </summary>
public enum FinishReason
{
    /// <summary>정상 완료</summary>
    Stop,
    
    /// <summary>최대 길이 도달</summary>
    Length,
    
    /// <summary>함수 호출</summary>
    FunctionCall,
    
    /// <summary>도구 호출</summary>
    ToolCalls,
    
    /// <summary>콘텐츠 필터</summary>
    ContentFilter,
    
    /// <summary>정지 단어</summary>
    StopWord,
    
    /// <summary>에러</summary>
    Error,
    
    /// <summary>취소됨</summary>
    Cancelled,
    
    /// <summary>알 수 없음</summary>
    Unknown
}