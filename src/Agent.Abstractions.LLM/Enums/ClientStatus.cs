namespace Agent.Abstractions.LLM.Enums;

/// <summary>
/// 클라이언트 상태
/// </summary>
public enum ClientStatus
{
    /// <summary>정상</summary>
    Healthy,
    
    /// <summary>경고</summary>
    Warning,
    
    /// <summary>에러</summary>
    Error,
    
    /// <summary>초기화 중</summary>
    Initializing,
    
    /// <summary>사용 불가</summary>
    Unavailable
}