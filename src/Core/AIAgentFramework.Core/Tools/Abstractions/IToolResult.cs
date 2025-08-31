namespace AIAgentFramework.Core.Tools.Abstractions;


/// <summary>
/// 도구 결과 인터페이스
/// </summary>
public interface IToolResult
{
    /// <summary>
    /// 성공 여부
    /// </summary>
    bool Success { get; }
    
    /// <summary>
    /// 결과 데이터
    /// </summary>
    Dictionary<string, object> Data { get; }
    
    /// <summary>
    /// 오류 메시지
    /// </summary>
    string? ErrorMessage { get; }
    
    /// <summary>
    /// 실행 시간
    /// </summary>
    TimeSpan ExecutionTime { get; }
    
    /// <summary>
    /// 메타데이터
    /// </summary>
    Dictionary<string, object> Metadata { get; }
}