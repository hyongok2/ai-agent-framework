namespace Agent.Abstractions.Core.Agents;


/// <summary>
/// 에러 타입
/// </summary>
public enum ErrorType
{
    /// <summary>알 수 없음</summary>
    Unknown,
    
    /// <summary>검증 실패</summary>
    Validation,
    
    /// <summary>인증 실패</summary>
    Authentication,
    
    /// <summary>권한 부족</summary>
    Authorization,
    
    /// <summary>리소스 없음</summary>
    NotFound,
    
    /// <summary>충돌</summary>
    Conflict,
    
    /// <summary>속도 제한</summary>
    RateLimit,
    
    /// <summary>타임아웃</summary>
    Timeout,
    
    /// <summary>서비스 불가</summary>
    ServiceUnavailable,
    
    /// <summary>내부 에러</summary>
    Internal,
    
    /// <summary>설정 에러</summary>
    Configuration,
    
    /// <summary>네트워크 에러</summary>
    Network
}