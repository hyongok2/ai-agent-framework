namespace Agent.Core.Abstractions.Llm.Enums;

/// <summary>
/// LLM 이벤트 타입
/// </summary>
public enum LlmEventType
{
    /// <summary>시작됨</summary>
    Started,
    
    /// <summary>처리 중</summary>
    Processing,
    
    /// <summary>완료됨</summary>
    Completed,
    
    /// <summary>에러 발생</summary>
    Error,
    
    /// <summary>취소됨</summary>
    Cancelled,
    
    /// <summary>속도 제한</summary>
    RateLimited,
    
    /// <summary>재시도</summary>
    Retry,
    
    /// <summary>캐시 히트</summary>
    CacheHit,
    
    /// <summary>모델 로딩</summary>
    ModelLoading,
    
    /// <summary>큐 대기</summary>
    Queued
}