namespace AIAgentFramework.Core.Interfaces;

/// <summary>
/// LLM 결과 인터페이스
/// </summary>
public interface ILLMResult
{
    /// <summary>
    /// 성공 여부
    /// </summary>
    bool Success { get; }
    
    /// <summary>
    /// 생성된 내용
    /// </summary>
    string Content { get; }
    
    /// <summary>
    /// 오류 메시지
    /// </summary>
    string? ErrorMessage { get; }
    
    /// <summary>
    /// 사용된 토큰 수
    /// </summary>
    int TokensUsed { get; }
    
    /// <summary>
    /// 사용된 모델
    /// </summary>
    string Model { get; }
    
    /// <summary>
    /// 실행 시간
    /// </summary>
    TimeSpan ExecutionTime { get; }
    
    /// <summary>
    /// 메타데이터
    /// </summary>
    Dictionary<string, object> Metadata { get; }
    
    /// <summary>
    /// 결과 데이터
    /// </summary>
    Dictionary<string, object> Data { get; }
}